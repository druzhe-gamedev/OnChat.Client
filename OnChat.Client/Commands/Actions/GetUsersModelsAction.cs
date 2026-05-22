using System.CommandLine;
using System.CommandLine.Invocation;
using Client.Connection;
using Client.User;
using Microsoft.Extensions.Logging;
using OnChat.Protocol.Packets;
using OnChat.Shared.Users;

namespace Client.Commands.Actions;

public class GetUsersModelsAction(ChatConnection connection, UsersProvider usersProvider, ILogger<GetUsersModelsAction> logger)
    : AsynchronousCommandLineAction
{
    public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken = default)
    {
        IResponse response = await connection.Request(new GetUsersModelsPacket(Guid.Empty), cancellationToken);

        if (response is not ReceiveUsersModelsPacket usersPacket)
        {
            logger.LogError("Error!");
            return 1;
        }

        foreach (UserModel userModel in usersPacket.UsersModels)
        {
            usersProvider.Users.AddOrUpdate(userModel.UserId, userModel, (_, _) => userModel);
            logger.LogInformation("[{UserModelUserId}] {UserModelUsername}", userModel.UserId, userModel.Username);
        }
        
        return 0;
    }
}