using System.Text;
using Client.Connection;
using Client.Keys;
using Client.User;
using Microsoft.Extensions.Logging;
using OnChat.Protocol.PacketHandler;
using OnChat.Protocol.Packets;
using OnChat.Shared.Encryption;
using OnChat.Shared.Messages;
using OnChat.Shared.Users;

namespace Client.Messages;

public class ReceiveMessageHandler(ChatConnection connection, UsersProvider usersProvider, KeysVault keysVault, ILogger<ReceiveMessageHandler> logger) : PacketHandler<ReceiveMessagePacket>
{
    protected override async Task<IResponse> Handle(ReceiveMessagePacket packet, IConnection caller)
    {
        if (caller.AuthenticationState is not Authenticated authenticated)
        {
            logger.LogError("Not authenticated");
            return null!;
        }

        if (!keysVault.TryGetKeysRecord(authenticated.UserId, out KeysRecord? keys))
        {
            logger.LogError("No key pair found");
            return null!;
        }

        StringBuilder sb = new();
        Guid senderId = packet.SenderId;
        if (!usersProvider.Users.TryGetValue(senderId, out UserModel? user))
        {
            IResponse response = await connection.Request(new GetUserModelPacket(Guid.Empty, senderId));

            if (response is ReceiveUserModelPacket userModel)
            {
                AppendUserInfo(userModel.UserModel.UserId, userModel.UserModel.Username);
                
                usersProvider.Users.TryAdd(senderId, userModel.UserModel);
            }
            else
                sb.Append($"No info for user with id [{senderId}]");
        }
        else
            AppendUserInfo(user.UserId, user.Username);
        
        string message = ECDHEncryption.DecryptMessage(packet.Message, authenticated.UserId, keys!.PrivateKey);
        sb.Append(message);
        
        logger.LogInformation(sb.ToString());
        return null!;
        
        void AppendUserInfo(Guid userId, string username) => sb.Append($"[{userId}] {username}: ");
    }
}