using Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AISmarteasy.Core.Function;

public abstract class AIServiceConnector : IAIServiceConnector
{
    protected ILogger Logger { get; set; }

    protected AIServiceConnector(ILogger? logger = null)
    {
        Logger = logger ?? NullLogger.Instance;
    }

    public abstract Task<ChatHistory> TextCompletionAsync(ChatHistory chatHistory, LLMServiceSetting requestSetting, CancellationToken cancellationToken = default);

    protected static async Task<T> RunAsync<T>(Func<Task<T>?> request)
    {
        try
        {
            return await request.Invoke()!.ConfigureAwait(false);
        }
        catch (RequestFailedException e)
        {
            throw e.ToHttpOperationException();
        }
    }

    protected static void ValidateMaxTokens(int? maxTokens)
    {
        if (maxTokens is < 1)
        {
            throw new CoreException($"MaxTokens {maxTokens} is not valid, the value must be greater than zero");
        }
    }
}
