using Iggy_SDK.Configuration;
using Iggy_SDK.Enums;
using Iggy_SDK.Exceptions;
using Iggy_SDK.MessageStream;
using Iggy_SDK.MessageStream.Implementations;
using System.ComponentModel;
using System.Net.Sockets;

namespace Iggy_SDK.Factory;

public static class MessageStreamFactory
{
    //TODO - this whole setup will have to be refactored later,when adding support for ASP.NET Core DI
    public static IIggyClient CreateMessageStream(Action<IMessageStreamConfigurator> options)
    {
        var config = new MessageStreamConfigurator();
        options.Invoke(config);
        
        return config.Protocol switch
        {
            Protocol.Http => CreateHttpMessageStream(config),
            Protocol.Tcp => CreateTcpMessageStream(config),
            _ => throw new InvalidEnumArgumentException()
        };
    }
    
    private static TcpMessageStream CreateTcpMessageStream(IMessageStreamConfigurator options)
    {
        var socket = CreateTcpSocket(options);
        return new TcpMessageStreamBuilder(socket, options)
        //this internally resolves whether the message dispatcher is created or not.
            .WithSendMessagesDispatcher()
            .Build();
    }

    private static Socket CreateTcpSocket(IMessageStreamConfigurator options)
    {
        var urlPortSplitter = options.BaseAdress.Split(":");
        if (urlPortSplitter.Length > 2)
        {
            throw new InvalidBaseAdressException();
        }
        
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Connect(urlPortSplitter[0], int.Parse(urlPortSplitter[1]));
        socket.SendBufferSize = options.SendBufferSize;
        socket.ReceiveBufferSize = options.ReceiveBufferSize;
        return socket;
    }

    private static HttpMessageStream CreateHttpMessageStream(IMessageStreamConfigurator options)
    {
        var client = CreateHttpClient(options);
        //this internally resolves whether the message dispatcher is created or not
        return new HttpMessageStreamBuilder(client, options)
            .WithSendMessagesDispatcher()
            .Build();
    }
    private static HttpClient CreateHttpClient(IMessageStreamConfigurator options)
    {
        var client = new HttpClient();
        client.BaseAddress = new Uri(options.BaseAdress);
        
        if (options.Headers is not null)
        {
            foreach (var header in options.Headers)
            {
                client.DefaultRequestHeaders.Add(header.Name, header.Values);
            }
        }
        return client;
    }
}