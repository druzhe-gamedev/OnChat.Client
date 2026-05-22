using System.CommandLine;
using System.CommandLine.Invocation;
using Client.Connection;
using Client.Keys;
using Client.Tokens;
using Microsoft.Extensions.Logging;
using OnChat.Protocol.PacketHandler;
using OnChat.Protocol.Packets;
using OnChat.Shared;
using OnChat.Shared.Encryption;

namespace Client.Commands.Actions;

public class GenerateKeysAction(
    ChatConnection connection,
    TokensService tokensService,
    KeysVault keysVault,
    ILogger<GenerateKeysAction> logger
) : AsynchronousCommandLineAction
{
    public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken = default)
    {
        if (!tokensService.TryGetJwtToken(out string? token) ||
            connection.AuthenticationState is not Authenticated authenticated) 
        {
            logger.LogError("No jwt token provided");
            return 1;
        }
        
        (byte[] publicKey, byte[] privateKey) keys = ECDHEncryption.GetKeys();
        PublicKeyPacket publicKeyPacket = new (Guid.Empty, token!, keys.publicKey);
        IResponse publishKeyRequest = await connection.Request<IResponse>(publicKeyPacket, cancellationToken: cancellationToken);
        
        // ReSharper disable once InvertIf
        if(publishKeyRequest is FailureResponse failureResponse)
        {
            logger.LogError(failureResponse.Description);
            return 1;
        }

        await keysVault.AddOrUpdateKeysRecord(authenticated.UserId, new KeysRecord(authenticated.UserId, keys.publicKey, keys.privateKey));
        return 0;
    }
}