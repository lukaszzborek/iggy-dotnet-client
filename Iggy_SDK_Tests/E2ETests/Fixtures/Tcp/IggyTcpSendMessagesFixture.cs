using DotNet.Testcontainers.Builders;
using Iggy_SDK;
using Iggy_SDK_Tests.Utils.Messages;
using Iggy_SDK.Configuration;
using Iggy_SDK.Contracts.Http;
using Iggy_SDK.MessagesDispatcher;
using Iggy_SDK.MessageStream.Implementations;
using Iggy_SDK_Tests.Utils.Streams;
using Iggy_SDK_Tests.Utils.Topics;
using Iggy_SDK.Enums;
using Iggy_SDK.Factory;
using Iggy_SDK.MessageStream;
using System.Net.Sockets;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using IContainer = DotNet.Testcontainers.Containers.IContainer;

namespace Iggy_SDK_Tests.E2ETests.Fixtures.Tcp;

public sealed class IggyTcpSendMessagesFixture : IAsyncLifetime
{
    private readonly IContainer _container = new ContainerBuilder().WithImage("iggyrs/iggy:latest")
        //.WithPortBinding(3000, true)
        .WithPortBinding(8090, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8090))
        //.WithPortBinding(8080, true)
        .Build();

    public IIggyClient sut;

    private static readonly StreamRequest StreamRequest = StreamFactory.CreateStreamRequest();
    private static readonly StreamRequest NonExistingStreamRequest = StreamFactory.CreateStreamRequest();
    private static readonly TopicRequest NonExistingTopicRequest = TopicFactory.CreateTopicRequest();
    private static readonly TopicRequest TopicRequest = TopicFactory.CreateTopicRequest();

    public readonly int StreamId = StreamRequest.StreamId;
    public readonly int TopicId = TopicRequest.TopicId;

    public readonly int InvalidStreamId = NonExistingStreamRequest.StreamId;
    public readonly int InvalidTopicId = NonExistingTopicRequest.TopicId;

    public readonly int PartitionId = 1;
    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        sut = MessageStreamFactory.CreateMessageStream(options =>
        {
            options.BaseAdress = $"127.0.0.1:{_container.GetMappedPublicPort(8090)}";
            options.Protocol = Protocol.Tcp;
            options.IntervalBatchingConfig = x =>
            {
                x.Enabled = false;
                x.Interval = TimeSpan.FromMilliseconds(100);
                x.MaxMessagesPerBatch = 1000;
                x.MaxRequests = 8912;
            };
            options.LoggerFactory = NullLoggerFactory.Instance;
        });

        await sut.CreateStreamAsync(StreamRequest);
        await sut.CreateTopicAsync(Identifier.Numeric(StreamRequest.StreamId), TopicRequest);
    }

    public async Task DisposeAsync()
    {
        await _container.StopAsync();
    }
}