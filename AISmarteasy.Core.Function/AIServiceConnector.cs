using Azure;
using Microsoft.Extensions.Logging;

namespace AISmarteasy.Core.Function;

public abstract class AIServiceConnector 
{
    protected ILogger Logger { get; set; } = LoggerProvider.Provide();
}
