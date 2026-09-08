namespace IChat.Infrastructure.Ai;

using System.ComponentModel.DataAnnotations;

public sealed class PipelineOptions
{
    public bool EnableCaching { get; set; } = true;

    public bool EnableOpenTelemetry { get; set; } = true;

    public bool EnableFunctionInvocation { get; set; }

    [Range(0, 10)]
    public int MaxRetryAttempts { get; set; } = 3;
}
