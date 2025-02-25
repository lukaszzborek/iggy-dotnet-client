using System.Buffers.Binary;
using System.Text;

namespace Iggy_SDK_Tests.Utils;

internal sealed class BinaryFactory
{
    internal static byte[] CreateOffsetPayload(int partitionId, ulong currentOffset, ulong offset)
    {
        var payload = new byte[20];
        BinaryPrimitives.WriteInt32LittleEndian(payload, partitionId);
        BinaryPrimitives.WriteUInt64LittleEndian(payload.AsSpan(4), currentOffset);
        BinaryPrimitives.WriteUInt64LittleEndian(payload.AsSpan(12), offset);
        return payload;
    }

    internal static byte[] CreateMessagePayload(ulong offset, ulong timestamp, int headersLength, uint checkSum, Guid guid, ReadOnlySpan<byte> payload)
    {
        var messageLength = payload.Length;
        var totalSize = 45 + payload.Length;
        var payloadBuffer = new byte[totalSize];


        BinaryPrimitives.WriteUInt64LittleEndian(payloadBuffer, (ulong)offset);
        payloadBuffer.AsSpan()[8] = (byte)10;
        BinaryPrimitives.WriteUInt64LittleEndian(payloadBuffer.AsSpan(9), (ulong)timestamp);
        var idBytes = guid.ToByteArray();
        idBytes.CopyTo(payloadBuffer.AsSpan()[17..33]);
        BinaryPrimitives.WriteUInt32LittleEndian(payloadBuffer.AsSpan(33), (uint)checkSum);
        BinaryPrimitives.WriteUInt32LittleEndian(payloadBuffer.AsSpan(37), (uint)headersLength);
        BinaryPrimitives.WriteUInt32LittleEndian(payloadBuffer.AsSpan(41), (uint)messageLength);
        payload.CopyTo(payloadBuffer.AsSpan(45));
        return payloadBuffer;
    }


    // int id = BinaryPrimitives.ReadInt32LittleEndian(payload[position..(position + 4)]);
    // ulong createdAt = BinaryPrimitives.ReadUInt64LittleEndian(payload[(position + 4)..(position + 12)]);
    // int topicsCount = BinaryPrimitives.ReadInt32LittleEndian(payload[(position + 12)..(position + 16)]);
    // ulong sizeBytes = BinaryPrimitives.ReadUInt64LittleEndian(payload[(position + 16)..(position + 24)]);
    // ulong messagesCount = BinaryPrimitives.ReadUInt64LittleEndian(payload[(position + 24)..(position + 32)]);
    // int nameLength = (int)payload[position + 32];
    internal static byte[] CreateStreamPayload(int id, int topicsCount, string name, ulong sizeBytes, ulong messagesCount, ulong createdAt)
    {
        var nameBytes = Encoding.UTF8.GetBytes(name);
        var totalSize = 4 + 4 + 8 + 8 + 1 + 8 + nameBytes.Length;
        var payload = new byte[totalSize];
        BinaryPrimitives.WriteInt32LittleEndian(payload, id);
        BinaryPrimitives.WriteUInt64LittleEndian(payload.AsSpan(4), createdAt);
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(12), topicsCount);
        BinaryPrimitives.WriteUInt64LittleEndian(payload.AsSpan(16), sizeBytes);
        BinaryPrimitives.WriteUInt64LittleEndian(payload.AsSpan(24), messagesCount);
        payload[32] = (byte)nameBytes.Length;
        nameBytes.CopyTo(payload.AsSpan(33));
        return payload;
    }
    internal static byte[] CreateTopicPayload(int id, int partitionsCount, int messageExpiry, string name, ulong sizeBytes, ulong messagesCount, ulong createdAt)
    {
        var nameBytes = Encoding.UTF8.GetBytes(name);
        var totalSize = 4 + 4 + 4 + 8 + 8 + 8 + 1 + nameBytes.Length;
        var payload = new byte[totalSize];
        BinaryPrimitives.WriteInt32LittleEndian(payload, id);
        BinaryPrimitives.WriteUInt64LittleEndian(payload.AsSpan(4), createdAt);
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(12), partitionsCount);
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(16), messageExpiry);
        BinaryPrimitives.WriteUInt64LittleEndian(payload.AsSpan(20), sizeBytes);
        BinaryPrimitives.WriteUInt64LittleEndian(payload.AsSpan(28), messagesCount);
        payload[36] = (byte)nameBytes.Length;
        nameBytes.CopyTo(payload.AsSpan(37));
        return payload;
    }

    internal static byte[] CreatePartitionPayload(int id, int segmentsCount, int currentOffset, ulong sizeBytes, ulong messagesCount)
    {
        var payload = new byte[16];
        BinaryPrimitives.WriteInt32LittleEndian(payload, id);
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(4), segmentsCount);
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(8), currentOffset);
        BinaryPrimitives.WriteUInt64LittleEndian(payload.AsSpan(12), sizeBytes);
        BinaryPrimitives.WriteUInt64LittleEndian(payload.AsSpan(16), messagesCount);
        return payload;
    }

    internal static byte[] CreateGroupPayload(int id, int membersCount, int partitionsCount)
    {
        var payload = new byte[12];
        BinaryPrimitives.WriteInt32LittleEndian(payload, id);
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(4), partitionsCount);
        BinaryPrimitives.WriteInt32LittleEndian(payload.AsSpan(8), membersCount);
        return payload;
    }

    internal static byte[] CreateStatsPayload(Iggy_SDK.Contracts.Http.Stats stats)
    {
        byte[] bytes = new byte[1024];

        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0, 4), stats.ProcessId);
        BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(4, 4), stats.CpuUsage);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(8, 8), stats.MemoryUsage);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(16, 8), stats.TotalMemory);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(24, 8), stats.AvailableMemory);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(32, 8), stats.RunTime);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(40, 8), (ulong)stats.StartTime.ToUnixTimeSeconds());
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(48, 8), stats.ReadBytes);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(56, 8), stats.WrittenBytes);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(64, 8), stats.MessagesSizeBytes);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(72, 4), stats.StreamsCount);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(76, 4), stats.TopicsCount);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(80, 4), stats.PartitionsCount);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(84, 4), stats.SegmentsCount);
        BinaryPrimitives.WriteUInt64LittleEndian(bytes.AsSpan(88, 8), stats.MessagesCount);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(96, 4), stats.ClientsCount);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(100, 4), stats.ConsumerGroupsCount);

        // Convert string properties to bytes and set them in the byte array
        byte[] hostnameBytes = Encoding.UTF8.GetBytes(stats.Hostname);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(104, 4), hostnameBytes.Length);
        hostnameBytes.CopyTo(bytes, 108);

        byte[] osNameBytes = Encoding.UTF8.GetBytes(stats.OsName);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(108 + hostnameBytes.Length, 4), osNameBytes.Length);
        osNameBytes.CopyTo(bytes, 112 + hostnameBytes.Length);

        byte[] osVersionBytes = Encoding.UTF8.GetBytes(stats.OsVersion);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(112 + hostnameBytes.Length + osNameBytes.Length, 4), osVersionBytes.Length);
        osVersionBytes.CopyTo(bytes, 116 + hostnameBytes.Length + osNameBytes.Length);

        byte[] kernelVersionBytes = Encoding.UTF8.GetBytes(stats.KernelVersion);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(116 + hostnameBytes.Length + osNameBytes.Length + osVersionBytes.Length, 4), kernelVersionBytes.Length);
        kernelVersionBytes.CopyTo(bytes, 120 + hostnameBytes.Length + osNameBytes.Length + osVersionBytes.Length);

        return bytes;
    }
}