using Iggy_SDK.Contracts.Http;
using Iggy_SDK.Enums;
using Microsoft.Extensions.Logging;

namespace Iggy_SDK.Configuration;

public sealed class MessageStreamConfigurator : IMessageStreamConfigurator
{
    public string BaseAdress { get; set; } = "http://127.0.0.1:3000";
    public Protocol Protocol { get; set; } = Protocol.Http;
    public IEnumerable<HttpRequestHeaderContract>? Headers { get; set; } = null;
    public ILoggerFactory? LoggerFactory { get; set; } = null;
    public Action<IntervalBatchingSettings> IntervalBatchingConfig { get; set; } = options =>
    {
        options.Interval = TimeSpan.FromMilliseconds(100);
        options.MaxMessagesPerBatch = 1000;
        options.MaxRequests = 4096;
    };
    public int ReceiveBufferSize { get; set; } = 4096;
    public int SendBufferSize { get; set; } = 4096;
}