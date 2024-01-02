﻿using System.Runtime.CompilerServices;
using Azure;
using Microsoft.Extensions.Logging;

namespace AISmarteasy.Core.Function;

public abstract class AIServiceConnector(ILogger logger) : IAIServiceConnector
{
    protected ILogger Logger { get; set; } = logger;

    public abstract Task<ChatHistory> TextCompletionAsync(ChatHistory chatHistory, LLMServiceSetting requestSetting, CancellationToken cancellationToken = default);
    public abstract IAsyncEnumerable<ChatStreamingResult> TextCompletionStreamingAsync(ChatHistory chatHistory, LLMServiceSetting requestSetting,
        CancellationToken cancellationToken = default);

    protected static async Task<T> RunAsync<T>(Func<Task<T>> request)
    {
        try
        {
            return await request.Invoke().ConfigureAwait(false);
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
