using System.CommandLine;
using System.CommandLine.Invocation;
using Client.Connection;
using Client.Keys;
using Client.Tokens;
using Client.User;
using Microsoft.Extensions.Logging;
using OnChat.Protocol.PacketHandler;
using OnChat.Protocol.Packets;
using OnChat.Shared.Encryption;
using OnChat.Shared.Messages;
using OnChat.Shared.Users;

namespace Client.Commands.Actions;

public class SendMessageAction(
    ChatConnection connection,
    TokensService tokensService,
    KeysVault keysVault,
    UsersProvider usersProvider,
    ILogger<SendMessageAction> logger
) : AsynchronousCommandLineAction
{
    public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken = default)
    {
        Guid userId = parseResult.GetValue<Guid>("-r");
        string? message = parseResult.GetValue<string>("-m");

        if (string.IsNullOrWhiteSpace(message))
        {
            logger.LogError("String arguments must not be empty or white spaces");
            return 1;
        }

        if (!usersProvider.Users.TryGetValue(userId, out UserModel? user))
        {
            logger.LogError("No user's public key info");
            return 1;
        }
        
        if (connection.AuthenticationState is not Authenticated authenticated)
        {
            logger.LogError("Not authenticated");
            return 1;
        }

        if (!keysVault.TryGetKeysRecord(authenticated.UserId, out KeysRecord? keys))
        {
            logger.LogError("No key pair found");
            return 1;
        }

        if (!tokensService.TryGetJwtToken(out string? token))
        {
            logger.LogError("No jwt token provided");
            return 1;
        }

        EncryptedMessage encryptedMessage = ECDHEncryption.EncryptMessage(
            [(userId, user.PublicKey), (authenticated.UserId, keys!.PublicKey)],
            authenticated.UserId,
            message
        );
        
        SendMessagePacket packet = new(Guid.Empty, token!, userId, encryptedMessage);
        
        IResponse response = await connection.Request<IResponse>(packet, cancellationToken: cancellationToken);
        
        if(response is WrongIdPacket failure)
            logger.LogError(failure.Description);

        return 0;
    }
}