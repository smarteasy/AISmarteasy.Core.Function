using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace AISmarteasy.Core.Function;

public sealed class NativeFunction(string pluginName, string name, string description, 
        IList<ParameterInfo> parameters, Func<string> skillFunction, ILogger logger) : PluginFunction(pluginName, name, description, false, parameters)
{
    public static PluginFunction FromNativeMethod(MethodInfo method, ILogger logger, object? target = null, string? pluginName = null)
    {
        if (!method.IsStatic && target is null)
        {
            throw new ArgumentNullException(nameof(target), "Argument cannot be null for non-static methods");
        }

        var methodDetails = GetMethodDetails(method, target, logger);

        return new NativeFunction(pluginName!, methodDetails.Name, methodDetails.Description, methodDetails.Parameters, methodDetails.Function, logger);
    }

    public override Task<ChatHistory> RunAsync(IAIServiceConnector serviceConnector, LLMServiceSetting serviceSetting,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var functionReturn = skillFunction();
            var chatHistory = new ChatHistory
            {
                PipelineLastContent = functionReturn
            };
            return Task.FromResult(chatHistory);
        }
        catch (Exception e) when (!e.IsCriticalException())
        {
            logger.LogError(e, "Native function {Plugin}.{Name} execution failed with error {Error}", PluginName, Name, e.Message);
            throw;
        }
    }

    private struct MethodDetails
    {
        public List<ParameterInfo> Parameters { get; set; }
        public string Name { get; init; }
        public string Description { get; init; }
        public Func<string> Function { get; set; }
    }

    private static MethodDetails GetMethodDetails(MethodInfo method, object? target, ILogger logger)
    {
        Verifier.NotNull(method);

        string? functionName = method.GetCustomAttribute<NameAttribute>(inherit: true)?.Name.Trim();
        if (string.IsNullOrEmpty(functionName))
        {
            functionName = SanitizeMetadataName(method.Name);
            Verifier.ValidFunctionName(functionName);

            if (IsAsyncMethod(method) &&
                functionName.EndsWith("Async", StringComparison.Ordinal) &&
                functionName.Length > "Async".Length)
            {
                functionName = functionName.Substring(0, functionName.Length - "Async".Length);
            }
        }

        method.GetCustomAttribute<FunctionAttribute>(inherit: true);

        string? description = method.GetCustomAttribute<DescriptionAttribute>(inherit: true)?.Description;

        var result = new MethodDetails
        {
            Name = functionName,
            Description = description ?? string.Empty,
        };

        (result.Function, result.Parameters) = GetDelegateInfo(target, method);

        logger.LogTrace("Method '{0}' found", result.Name);

        return result;
    }

    private static bool IsAsyncMethod(MethodInfo method)
    {
        Type t = method.ReturnType;

        if (t == typeof(Task) || t == typeof(ValueTask))
        {
            return true;
        }

        if (t.IsGenericType)
        {
            t = t.GetGenericTypeDefinition();
            if (t == typeof(Task<>) || t == typeof(ValueTask<>))
            {
                return true;
            }
        }

        return false;
    }

    private static (Func<string> function, List<ParameterInfo>) GetDelegateInfo(object? instance, MethodInfo method)
    {
        ThrowForInvalidSignatureIf(method.IsGenericMethodDefinition, method, "Generic methods are not supported");

        var stringParameterViews = new List<ParameterInfo>();
        var parameters = method.GetParameters();

        var parameterFuncs = new Func<IWorkerContext,object?>[parameters.Length];
        bool sawFirstParameter = false, hasContextParam = false, hasLoggerParam = false, hasCultureParam = false;
        for (int i = 0; i < parameters.Length; i++)
        {
            (parameterFuncs[i], ParameterInfo? parameterView) = GetParameterMarshalerDelegate(method, parameters[i],
                ref sawFirstParameter, ref hasContextParam, ref hasLoggerParam, ref hasCultureParam);

            if (parameterView is not null)
            {
                stringParameterViews.Add(parameterView);
            }
        }

      
        string Function()
        {
            object?[] args = parameterFuncs.Length != 0 ? new object?[parameterFuncs.Length] : Array.Empty<object?>();
            
            for (var i = 0; i < args.Length; i++)
            {
                args[i] = parameterFuncs[i](LLMWorkEnv.WorkerContext);
            }

            var result = (string)method.Invoke(instance, args)!;

            return result;
        }

        stringParameterViews.AddRange(method
            .GetCustomAttributes<ParameterAttribute>(inherit: true)
            .Select(x => new ParameterInfo(x.Name, x.Description, x.DefaultValue ?? string.Empty)));

        Verifier.ParametersUniqueness(stringParameterViews);

        return (Function, stringParameterViews);
    }

    private static (Func<IWorkerContext, object?>, ParameterInfo?) GetParameterMarshalerDelegate(
        MethodInfo method, System.Reflection.ParameterInfo parameter, ref bool sawFirstParameter, ref bool hasContextParam, ref bool hasLoggerParam, ref bool hasCultureParam)
    {
        Type type = parameter.ParameterType;


        if (type == typeof(IWorkerContext))
        {
            TrackUniqueParameterType(ref hasContextParam, method, $"At most one {nameof(IWorkerContext)} parameter is permitted.");
            return (static (context) => context, null);
        }

        if (type == typeof(ILogger))
        {
            TrackUniqueParameterType(ref hasLoggerParam, method, $"At most one {nameof(ILogger)}/{nameof(ILoggerFactory)} parameter is permitted.");
            return (static (context) => context, null);
        }

        if (type == typeof(CultureInfo) || type == typeof(IFormatProvider))
        {
            TrackUniqueParameterType(ref hasCultureParam, method, $"At most one {nameof(CultureInfo)}/{nameof(IFormatProvider)} parameter is permitted.");
            return (static (context) => context.Culture, null);
        }

        if (!type.IsByRef && GetParser(type) is { } parser)
        {
            NameAttribute? nameAttr = parameter.GetCustomAttribute<NameAttribute>(inherit: true);
            string name = nameAttr?.Name.Trim() ?? SanitizeMetadataName(parameter.Name);
            bool nameIsInput = name.Equals("input", StringComparison.OrdinalIgnoreCase);
            ThrowForInvalidSignatureIf(name.Length == 0, method, $"Parameter {parameter.Name}'s context attribute defines an invalid name.");
            ThrowForInvalidSignatureIf(sawFirstParameter && nameIsInput, method, "Only the first parameter may be named 'input'");

            DefaultValueAttribute? defaultValueAttribute = parameter.GetCustomAttribute<DefaultValueAttribute>(inherit: true);
            bool hasDefaultValue = defaultValueAttribute is not null;
            object? defaultValue = defaultValueAttribute?.Value;
            if (!hasDefaultValue && parameter.HasDefaultValue)
            {
                hasDefaultValue = true;
                defaultValue = parameter.DefaultValue;
            }

            if (hasDefaultValue)
            {
                if (defaultValue is string defaultStringValue && defaultValue.GetType() != typeof(string))
                {
                    defaultValue = parser(defaultStringValue, CultureInfo.InvariantCulture);
                }
                else
                {
                    ThrowForInvalidSignatureIf(
                        defaultValue is null && type.IsValueType && Nullable.GetUnderlyingType(type) is null,
                        method,
                        $"Type {type} is a non-nullable value type but a null default value was specified.");
                    ThrowForInvalidSignatureIf(
                        defaultValue is not null && !type.IsInstanceOfType(defaultValue),
                        method,
                        $"Default value {defaultValue} for parameter {name} is not assignable to type {type}.");
                }
            }

            bool fallBackToInput = !sawFirstParameter && !nameIsInput;

            object? ParameterFunc(IWorkerContext context)
            {
                if (context.Variables.TryGetValue(name, out string? value))
                {
                    return Process(value);
                }

                if (hasDefaultValue)
                {
                    return defaultValue;
                }

                if (fallBackToInput)
                {
                    return Process(context.Variables.Input);
                }

                throw new CoreException($"Missing value for parameter '{name}'");

                object? Process(string str)
                {
                    if (type == typeof(string))
                    {
                        return str;
                    }

                    try
                    {
                        return parser(str, context.Culture);
                    }
                    catch (Exception e) when (!e.IsCriticalException())
                    {
                        throw new ArgumentOutOfRangeException(name, str, e.Message);
                    }
                }
            }

            sawFirstParameter = true;

            var parameterView = new ParameterInfo(
                name,
                parameter.GetCustomAttribute<DescriptionAttribute>(inherit: true)?.Description ?? string.Empty,
                defaultValue?.ToString() ?? string.Empty);

            return (ParameterFunc, parameterView);
        }

        throw GetExceptionForInvalidSignature(method, $"Unknown parameter type {parameter.ParameterType}");
    }

    [DoesNotReturn]
    private static Exception GetExceptionForInvalidSignature(MethodInfo method, string reason) =>
        throw new CoreException($"Function '{method.Name}' is not supported by the kernel. {reason}");

    private static void ThrowForInvalidSignatureIf([DoesNotReturnIf(true)] bool condition, MethodInfo method, string reason)
    {
        if (condition)
        {
            throw GetExceptionForInvalidSignature(method, reason);
        }
    }

    private static void TrackUniqueParameterType(ref bool hasParameterType, MethodInfo method, string failureMessage)
    {
        ThrowForInvalidSignatureIf(hasParameterType, method, failureMessage);
        hasParameterType = true;
    }

    private static Func<string, CultureInfo, object?>? GetParser(Type targetType) =>
        Parsers.GetOrAdd(targetType, static targetType =>
        {
            if (targetType == typeof(string))
            {
                return (input, _) => input;
            }

            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                targetType = Nullable.GetUnderlyingType(targetType) ?? throw new InvalidOperationException();
            }

            if (targetType.IsEnum)
            {
                return (input, _) => Enum.Parse(targetType, input, ignoreCase: true);
            }
            if (GetTypeConverter(targetType) is { } converter && converter.CanConvertFrom(typeof(string)))
            {
                return (input, cultureInfo) =>
                {
                    try
                    {
                        return converter.ConvertFromString(context: null, cultureInfo, input) ?? throw new InvalidOperationException();
                    }
                    catch (Exception e) when (!e.IsCriticalException() && !Equals(cultureInfo, CultureInfo.InvariantCulture))
                    {
                        return converter.ConvertFromInvariantString(input) ?? throw new InvalidOperationException();
                    }
                };
            }
            return null;
        });

    private static Func<object?, CultureInfo, string?>? GetFormatter(Type targetType) =>
        Formatters.GetOrAdd(targetType, static targetType =>
        {
            bool wasNullable = false;
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                wasNullable = true;
                targetType = Nullable.GetUnderlyingType(targetType) ?? throw new InvalidOperationException();
            }

            if (targetType.IsEnum)
            {
                return (input, _) => input?.ToString()!;
            }

            if (targetType == typeof(string))
            {
                return (input, _) => (string)input!;
            }

            if (GetTypeConverter(targetType) is { } converter && converter.CanConvertTo(typeof(string)))
            {
                return (input, cultureInfo) =>
                {
                    if (wasNullable && input is null)
                    {
                        return null!;
                    }

                    return converter.ConvertToString(context: null, cultureInfo, input);
                };
            }

            return null;
        });

    private static TypeConverter? GetTypeConverter(Type targetType)
    {
        if (targetType == typeof(byte)) { return new ByteConverter(); }
        if (targetType == typeof(sbyte)) { return new SByteConverter(); }
        if (targetType == typeof(bool)) { return new BooleanConverter(); }
        if (targetType == typeof(ushort)) { return new UInt16Converter(); }
        if (targetType == typeof(short)) { return new Int16Converter(); }
        if (targetType == typeof(char)) { return new CharConverter(); }
        if (targetType == typeof(uint)) { return new UInt32Converter(); }
        if (targetType == typeof(int)) { return new Int32Converter(); }
        if (targetType == typeof(ulong)) { return new UInt64Converter(); }
        if (targetType == typeof(long)) { return new Int64Converter(); }
        if (targetType == typeof(float)) { return new SingleConverter(); }
        if (targetType == typeof(double)) { return new DoubleConverter(); }
        if (targetType == typeof(decimal)) { return new DecimalConverter(); }
        if (targetType == typeof(TimeSpan)) { return new TimeSpanConverter(); }
        if (targetType == typeof(DateTime)) { return new DateTimeConverter(); }
        if (targetType == typeof(DateTimeOffset)) { return new DateTimeOffsetConverter(); }
        if (targetType == typeof(Uri)) { return new UriTypeConverter(); }
        if (targetType == typeof(Guid)) { return new GuidConverter(); }

        if (targetType.GetCustomAttribute<TypeConverterAttribute>() is { } tca &&
            Type.GetType(tca.ConverterTypeName, throwOnError: false) is { } converterType &&
            Activator.CreateInstance(converterType) is TypeConverter converter)
        {
            return converter;
        }

        return null;
    }

    private static string SanitizeMetadataName(string? methodName) =>
        InvalidNameCharsRegex.Replace(methodName!, "_");

    private static readonly Regex InvalidNameCharsRegex = new("[^0-9A-Za-z_]");

    private static readonly ConcurrentDictionary<Type, Func<string, CultureInfo, object>?> Parsers = new();

    private static readonly ConcurrentDictionary<Type, Func<object?, CultureInfo, string?>?> Formatters = new();
}
