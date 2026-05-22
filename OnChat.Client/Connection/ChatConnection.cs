using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Microsoft.Extensions.Logging;
using OnChat.Protocol;
using OnChat.Protocol.PacketHandler;
using OnChat.Protocol.Packets;
using OnChat.Shared.Messages;

namespace Client.Connection;

public class ChatConnection(IServiceProvider serviceProvider, ILogger<ChatConnection> logger) : IConnection
{
    private readonly BinaryProtocol _protocol = new(serviceProvider, typeof(SendMessagePacket).Assembly);
    private readonly TcpClient _client = new();
    private NetworkStream _stream = null!;
    private BinaryReader _reader = null!;
    private BinaryWriter _writer = null!;
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<IPacket>> _pendingRequests = new();
    private readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(5);

    public AuthenticationState AuthenticationState { get; private set; } = new NotAuthenticated();
    
    public async Task Connect()
    {
        _protocol.Setup();
        await _client.ConnectAsync(IPAddress.Loopback, 7596);

        _stream = _client.GetStream();
        _writer = new BinaryWriter(_stream);
        _reader = new BinaryReader(_stream);

        _ = Task.Run(Read);
    }

    public async Task<IResponse> Request(IPacket request, CancellationToken cancellationToken = default)
    {
        request.CorrelationId = Guid.CreateVersion7();
        
        var tcs = new TaskCompletionSource<IPacket>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingRequests[request.CorrelationId] = tcs;

        await Write(request);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_defaultTimeout);

        await using (cts.Token.Register(() => tcs.TrySetException(new TimeoutException($"No response for {request.GetType().Name}"))))
        {
            var responsePacket = await tcs.Task;
            return (IResponse)responsePacket;
        }
    }

    public async Task Write(IPacket packet)
    {
        MemoryStream ms = new();
        BinaryWriter writer = new(ms);
        
        using ProtocolBuffer buffer = new (new MemoryStream());

        buffer.Writer.Write((byte)packet.GetType().GetCustomAttribute<PacketIdAttribute>()!.PacketId);
        _protocol
              .GetCodec(packet.GetType())
              .Encode(buffer.Writer, packet);
        
        await buffer.WrapPacket(writer);
        ms.Seek(0, SeekOrigin.Begin);
        await ms.CopyToAsync(_stream);
    }
    
    public void Authenticate(Guid userId, string username)
    {
        if (AuthenticationState is not NotAuthenticated)
            return;
        
        logger.LogInformation("Logging in as [{UserId}] {Username}", userId, username);
        AuthenticationState = new Authenticated(userId, username, this);
    }

    public async Task Read()
    {
        try
        {
            while (true)
            {
                if (!_stream.CanRead) continue;
                using ProtocolBuffer buffer = await ProtocolBuffer.CreateFromReader(_reader);
                PacketId packetId = (PacketId)buffer.Reader.ReadByte();

                if (!_protocol.Packets.TryGetValue(packetId, out Type? packetType))
                {
                    logger.LogInformation("No packet handler for {PacketId}", packetId);
                    continue;
                }

                object packet = _protocol.GetCodec(packetType).Decode(buffer.Reader);

                if (packet is IResponse response)
                {
                    if (_pendingRequests.TryRemove(response.CorrelationId, out var cts))
                        cts.SetResult(response);
                    else
                    {
                        if (!_protocol.Handlers.TryGetValue(packet.GetType(), out IPacketHandler? packetHandler))
                        {
                            logger.LogInformation("Packet wasn't handled");
                            return;
                        }
                        
                        await packetHandler.Handle(response, this);
                    }
                }
                else
                    logger.LogError("Packet is malformed");
            }
        }
        catch (Exception e)
        {
            logger.LogInformation(e.Message);
        }
        finally
        {
            _client.Close();
        }
    }
}