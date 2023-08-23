
namespace Iggy_SDK.MessageStream;

public interface IMessageStream : IStreamClient, ITopicClient, IMessageClient, IOffsetClient, IConsumerGroupClient,
	IUtilsClient, IPartitionClient
{
}
