using System.CommandLine;
using System.CommandLine.Invocation;
using Client.Connection;
using OnChat.Protocol.Packets;
using OnChat.Shared.Auth;

namespace Client.Commands.Actions;

public class RegistrationAction(ChatConnection connection) : AsynchronousCommandLineAction
{
    public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken = default)
    {
        RegistrationPacket packet = new(
            Guid.Empty,
            parseResult.GetValue<string>("user")!,
            parseResult.GetValue<string>("mail")!,
            parseResult.GetValue<string>("pwd")!,
            parseResult.GetValue<short>("age")
        );
        
        IResponse response = await connection.Request<IResponse>(packet, cancellationToken: cancellationToken);
        
        if(response is RegistrationSuccessfulResponse success)
            Console.WriteLine("Account created");
        else if(response is RegistrationFailureResponse failure)
            Console.WriteLine($"{failure.Description}");

        return 0;
    }
}