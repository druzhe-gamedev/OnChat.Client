using System.CommandLine;
using System.CommandLine.Invocation;
using Client.Connection;
using Client.Extensions;
using Microsoft.Extensions.Logging;
using OnChat.Protocol.Packets;
using OnChat.Shared;
using OnChat.Shared.Auth;

namespace Client.Commands.Actions;

public class RegistrationAction(ChatConnection connection, ILogger<RegistrationAction> logger) : AsynchronousCommandLineAction
{
    public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken = default)
    {
        string? login = parseResult.GetValue<string>("-u");
        string? email = parseResult.GetValue<string>("-m");
        string? password = parseResult.GetValue<string>("-p");
        short age = parseResult.GetValue<short>("-a");

        if (string.IsNullOrWhiteSpaces(login, email, password))
        {
            logger.LogInformation("String arguments must not be empty or white spaces");
            return 1;
        }
        
        RegistrationPacket packet = new(Guid.Empty, login, email, password, age);
        
        IResponse response = await connection.Request<IResponse>(packet, cancellationToken: cancellationToken);
        
        if(response is RegistrationSuccessfulResponse)
            logger.LogInformation("Account created");
        else if(response is FailureResponse failure)
            logger.LogInformation("{FailureDescription}", failure.Description);

        return 0;
    }
}