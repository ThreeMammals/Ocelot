// Copyright © Kubernetes C# Client
// Repository: https://github.com/kubernetes-client/csharp
// Class: https://github.com/kubernetes-client/csharp/blob/master/tests/KubernetesClient.Tests/Mock/MockWebSocket.cs

using Nito.AsyncEx;
using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace Ocelot.UnitTests.WebSockets;

internal class MockWebSocket : WebSocket
{
    private WebSocketCloseStatus? closeStatus;
    private string closeStatusDescription;
    private WebSocketState state;
    private readonly string subProtocol;
    private readonly ConcurrentQueue<MessageData> receiveBuffers = new ConcurrentQueue<MessageData>();
    private readonly AsyncAutoResetEvent receiveEvent = new AsyncAutoResetEvent(false);
    private bool disposedValue;

    public MockWebSocket(string subProtocol = null)
    {
        this.subProtocol = subProtocol;
    }

    public void SetState(WebSocketState state)
    {
        this.state = state;
    }

    public EventHandler<MessageDataEventArgs> MessageSent { get; set; }

    public Task InvokeReceiveAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage)
    {
        receiveBuffers.Enqueue(new MessageData()
        {
            Buffer = buffer,
            MessageType = messageType,
            EndOfMessage = endOfMessage,
        });
        receiveEvent.Set();
        return Task.CompletedTask;
    }

    public override WebSocketCloseStatus? CloseStatus => closeStatus;

    public override string CloseStatusDescription => closeStatusDescription;

    public override WebSocketState State => state;

    public override string SubProtocol => subProtocol;

    public override void Abort()
    {
        throw new NotImplementedException();
    }

    public override Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription,
        CancellationToken cancellationToken)
    {
        this.closeStatus = closeStatus;
        closeStatusDescription = statusDescription;
        receiveBuffers.Enqueue(new MessageData()
        {
            Buffer = new ArraySegment<byte>(new byte[] { }),
            EndOfMessage = true,
            MessageType = WebSocketMessageType.Close,
        });
        receiveEvent.Set();
        return Task.CompletedTask;
    }

    public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public override async Task<WebSocketReceiveResult> ReceiveAsync(
        ArraySegment<byte> buffer,
        CancellationToken cancellationToken)
    {
        if (receiveBuffers.IsEmpty)
        {
            await receiveEvent.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        var bytesReceived = 0;
        var endOfMessage = true;
        var messageType = WebSocketMessageType.Close;

        MessageData received = null;
        if (receiveBuffers.TryPeek(out received))
        {
            messageType = received.MessageType;
            if (received.Buffer.Count <= buffer.Count)
            {
                receiveBuffers.TryDequeue(out received);
                received.Buffer.CopyTo(buffer);
                bytesReceived = received.Buffer.Count;
                endOfMessage = received.EndOfMessage;
            }
            else
            {
                received.Buffer.Slice(0, buffer.Count).CopyTo(buffer);
                bytesReceived = buffer.Count;
                endOfMessage = false;
                received.Buffer = received.Buffer.Slice(buffer.Count);
            }
        }

        return new WebSocketReceiveResult(bytesReceived, messageType, endOfMessage);
    }

    public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage,
        CancellationToken cancellationToken)
    {
        MessageSent?.Invoke(
            this,
            new MessageDataEventArgs()
            {
                Data = new MessageData()
                {
                    Buffer = buffer,
                    MessageType = messageType,
                    EndOfMessage = endOfMessage,
                },
            });
        return Task.CompletedTask;
    }

    public class MessageData
    {
        public ArraySegment<byte> Buffer { get; set; }
        public WebSocketMessageType MessageType { get; set; }
        public bool EndOfMessage { get; set; }
    }

    public class MessageDataEventArgs : EventArgs
    {
        public MessageData Data { get; set; }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                receiveBuffers.Clear();
                receiveEvent.Set();
            }

            disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~MockWebSocket()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }
    public override void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
