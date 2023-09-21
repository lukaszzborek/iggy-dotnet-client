using Iggy_SDK.Configuration;
using Iggy_SDK.Contracts.Http;
using Iggy_SDK.Enums;
using Iggy_SDK.Messages;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Buffers.Binary;
using System.Threading.Channels;

namespace Iggy_SDK.MessagesDispatcher;

internal sealed class MessageSenderDispatcher
{
    private readonly PeriodicTimer _timer;
    private readonly ILogger<MessageSenderDispatcher> _logger;
    private Task? _timerTask;
    private readonly CancellationTokenSource _cts = new();
    private readonly IMessageInvoker _messageInvoker;
    private readonly Channel<MessageSendRequest> _channel;
    private readonly int _maxMessages;
    private readonly int _maxRequestsInPoll;

    internal MessageSenderDispatcher(IntervalBatchingSettings sendMessagesOptions, Channel<MessageSendRequest> channel,
        IMessageInvoker messageInvoker, ILoggerFactory loggerFactory)
    {
        _timer = new (sendMessagesOptions.Interval);
        _logger = loggerFactory.CreateLogger<MessageSenderDispatcher>();
        _messageInvoker = messageInvoker;
        _maxMessages = sendMessagesOptions.MaxMessagesPerBatch;
        _maxRequestsInPoll = sendMessagesOptions.MaxRequests;
        _channel = channel;
    }
    internal void Start()
    {
        _timerTask = SendMessages();
    }
    internal async Task SendMessages()
    {
        var messagesSendRequests = new MessageSendRequest[_maxRequestsInPoll];
        while (await _timer.WaitForNextTickAsync(_cts.Token))
        {
            int idx = 0;
            while (_channel.Reader.TryRead(out var msg))
            {
                messagesSendRequests[idx++] = msg;
            }

            if (idx == 0)
            {
                continue;
            }

            var canBatchMessages = CanBatchMessages(messagesSendRequests.AsSpan()[..idx]);
            if (!canBatchMessages)
            {
                for (int i = 0; i < idx; i++)
                {
                    try
                    {
                        await _messageInvoker.SendMessagesAsync(messagesSendRequests[i], token: _cts.Token);
                    }
                    catch
                    {
                        var partId = BinaryPrimitives.ReadInt32LittleEndian(messagesSendRequests[i].Partitioning.Value);
                       _logger.LogError("Error encountered while sending messages - Stream ID:{streamId}, Topic ID:{topicId}, Partition ID: {partitionId}",
                           messagesSendRequests[i].StreamId, messagesSendRequests[i].TopicId, partId); 
                    }
                }

                continue;
            }

            var messagesBatches = BatchMessages(messagesSendRequests.AsSpan()[..idx]);
            foreach (var messages in messagesBatches)
            {
                try
                {
                    await _messageInvoker.SendMessagesAsync(messages, _cts.Token);
                }
                catch
                {
                        var partId = BinaryPrimitives.ReadInt32LittleEndian(messages.Partitioning.Value);
                       _logger.LogError("Error encountered while sending messages - Stream ID:{streamId}, Topic ID:{topicId}, Partition ID: {partitionId}",
                           messages.StreamId, messages.TopicId, partId); 
                }
            }
        }
    }
    private static bool CanBatchMessages(ReadOnlySpan<MessageSendRequest> requests)
    {
        for (int i = 0; i < requests.Length - 1; i++)
        {
            var start = requests[i];
            var next = requests[i + 1];

            if (!start.StreamId.Equals(next.StreamId)
                || !start.TopicId.Equals(next.TopicId)
                || start.Partitioning.Kind is not PartitioningKind.PartitionId
                || !start.Partitioning.Value.SequenceEqual(next.Partitioning.Value))
            {
                return false;
            }
        }
        return true;
    }

    private MessageSendRequest[] BatchMessages(Span<MessageSendRequest> requests)
    {
        int messagesCount = 0;
        for (int i = 0; i < requests.Length; i++)
        {
            messagesCount += requests[i].Messages.Count;
        }
        int batchesCount = (int)Math.Ceiling((decimal)messagesCount / _maxMessages);

        var messagesBuffer = ArrayPool<Message>.Shared.Rent(_maxMessages);
        var messages = messagesBuffer.AsSpan()[.._maxMessages];
        var messagesBatchesBuffer = MemoryPool<MessageSendRequest>.Shared.Rent(batchesCount);

        int idx = 0;
        int batchCounter = 0;
        try
        {
            foreach (var request in requests)
            {
                foreach (var message in request.Messages)
                {
                    messages[idx++] = message;
                    if (idx >= _maxMessages)
                    {
                        var messageSendRequest = new MessageSendRequest
                        {
                            Partitioning = request.Partitioning,
                            StreamId = request.StreamId,
                            TopicId = request.TopicId,
                            Messages = messages.ToArray()
                        };
                        messagesBatchesBuffer.Memory.Span[batchCounter] = messageSendRequest;
                        batchCounter++;
                        idx = 0;
                        messages.Clear();
                    }
                }
            }

            if (!messages.IsEmpty)
            {
                var messageSendRequest = new MessageSendRequest
                {
                    Partitioning = requests[0].Partitioning,
                    StreamId = requests[0].StreamId,
                    TopicId = requests[0].TopicId,
                    Messages = messages[..idx].ToArray()
                };
                messagesBatchesBuffer.Memory.Span[batchCounter++] = messageSendRequest;
            }
            return messagesBatchesBuffer.Memory.Span[..batchCounter].ToArray();
        }
        finally
        {
            ArrayPool<Message>.Shared.Return(messagesBuffer);
            messagesBatchesBuffer.Dispose();
        }

    }
    internal async Task StopAsync()
    {
        if (_timerTask is null)
        {
            return;
        }
        _timer.Dispose();
        _cts.Cancel();
        await _timerTask;
        _cts.Dispose();
    }

}