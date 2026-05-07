using System.CommandLine;
using System.CommandLine.Invocation;
using Client.Connection;
using OnChat.Protocol.Packets;
using OnChat.Shared.Auth;
using Serilog;

namespace Client.Commands.Actions;

public class AuthenticationAction(ChatConnection connection) : AsynchronousCommandLineAction
{
    public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken = default)
    {
        string? login = parseResult.GetValue<string>("login");
        string? password = parseResult.GetValue<string>("pwd");

        if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
        {
            Log.Error("login and password must have non-zero length");
            return 1;
        }
        
        AuthenticationPacket packet = new(Guid.Empty, login, password);
        
        IResponse response = await connection.Request<IResponse>(packet, cancellationToken: cancellationToken);
        
        if(response is TokensPacket success)
        {
            Console.WriteLine($"Logged in {success.AccessToken}\n{success.RefreshToken}");
        }

        return 0;
    }
}