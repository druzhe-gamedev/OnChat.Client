using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text;
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

public class LoadMessagesAction(
    ChatConnection connection,
    TokensService tokensService,
    UsersProvider usersProvider,
    KeysVault keysVault,
    ILogger<LoadMessagesAction> logger
) : AsynchronousCommandLineAction
{
    public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken = default)
    {
        Guid id = parseResult.GetValue<Guid>("-i");
        int page = parseResult.GetValue<int>("-p");
        int quantity = parseResult.GetValue<int>("-q");
        
        if (!tokensService.TryGetJwtToken(out string? token))
        {
            logger.LogError("No jwt token provided");
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

        IResponse response = await connection.Request<IResponse>(
            new LoadMessagesPacket(Guid.Empty, token!, id, quantity, page),
            cancellationToken
        );

        if (response is MessagesPacket messagesResponse)
        {
            StringBuilder sb = new();
            sb.Append('\n');
            
            foreach (EncryptedMessage encryptedMessage in messagesResponse.Messages)
            {
                string message = ECDHEncryption.DecryptMessage(encryptedMessage, authenticated.UserId, keys!.PrivateKey);

                Guid senderId = encryptedMessage.SenderId;
                if (!usersProvider.Users.TryGetValue(senderId, out UserModel? user))
                {
                    IResponse usersResponse = await connection.Request<IResponse>(
                        new GetUserModelPacket(Guid.Empty, senderId),
                        cancellationToken
                    );

                    if (usersResponse is ReceiveUserModelPacket userModel)
                    {
                        usersProvider.Users.TryAdd(senderId, userModel.UserModel);
                        user = userModel.UserModel;
                    }

                }
                sb.Append('[').Append(encryptedMessage.SenderId).Append("] ").Append(user?.Username ?? "unknown").Append(": ");
                sb.Append(message).Append(" [").Append(encryptedMessage.TimeStamp).Append("] \n");
            }
            
            logger.LogInformation(sb.ToString());
        }

        return 0;
    }
}