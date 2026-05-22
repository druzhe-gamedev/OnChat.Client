using System.CommandLine;
using System.CommandLine.Invocation;
using Client.Connection;
using Client.Extensions;
using Client.Tokens;
using Microsoft.Extensions.Logging;
using OnChat.Protocol.Packets;
using OnChat.Shared;
using OnChat.Shared.Auth;

namespace Client.Commands.Actions;

public class AuthenticationAction(
    ChatConnection connection,
    TokensService tokensService,
    ILogger<AuthenticationAction> logger
) : AsynchronousCommandLineAction
{
    public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken = default)
    {
        string? login = parseResult.GetValue<string>("-l");
        string? password = parseResult.GetValue<string>("-p");

        if (string.IsNullOrWhiteSpaces(login, password))
        {
            logger.LogError("login and password must have non-zero length");
            return 1;
        }
        
        AuthenticationPacket packet = new(Guid.Empty, login, password);
        
        IResponse response = await connection.Request(packet, cancellationToken: cancellationToken);

        switch (response)
        {
            case TokensPacket tokens:
                tokensService.Login(tokens.AccessToken, tokens.RefreshToken);
                break;
            case FailureResponse failure:
                Console.WriteLine(failure.Description);
                return 1;
        }

        return 0;
    }
}