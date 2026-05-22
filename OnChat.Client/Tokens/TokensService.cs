using Client.Connection;
using JWT.Algorithms;
using JWT.Builder;
using Microsoft.Extensions.Logging;
using OnChat.Protocol.Packets;
using OnChat.Shared.Auth;

namespace Client.Tokens;

public class TokensService(ChatConnection connection, ILogger<TokensService> logger)
{
    public TokensState TokensState { get; private set; } = new TokensEmpty();
    public bool IsLoggedIn { get; private set; }
    public readonly CancellationTokenSource LoginCancellationTokenSource = new();

    public void Login(TokenModel accessToken, TokenModel refreshToken)
    {
        if (IsLoggedIn)
            return;

        Dictionary<string, object> payload = JwtBuilder.Create()
                                                       .DoNotVerifySignature()
                                                       .WithAlgorithm(new HMACSHA256Algorithm())
                                                       .Decode<Dictionary<string, object>>(accessToken.Token);
        
        if (!payload.TryGetValue("username", out object? username) || username is not string usernameStr)
        {
            logger.LogError("Malformed JWT");
            return;
        }

        TokensState = new UserTokens(accessToken, refreshToken);
        IsLoggedIn = true;
        connection.Authenticate(Guid.Parse(payload["id"].ToString()!), usernameStr);
        
        _ = Task.Run(async () => await RotateTokens(LoginCancellationTokenSource.Token));
    }

    public bool TryGetJwtToken(out string? token)
    {
        token = "";
        
        if (TokensState is not UserTokens userTokens)
            return false;

        token = userTokens.AccessToken.Token;
        return true;
    }
    
    public bool TryGetRefreshToken(out string? token)
    {
        token = "";
        
        if (TokensState is not UserTokens userTokens)
            return false;

        token = userTokens.RefreshToken.Token;
        return true;
    }

    private async Task RotateTokens(CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(295), ct);

            if (!TryGetRefreshToken(out string? token))
                return;

            IResponse response = await connection.Request(new TokensRotationPacket(Guid.Empty, token!), ct);

            switch (response)
            {
                case TokensPacket tokensPacket: 
                    TokensState = new UserTokens(tokensPacket.AccessToken, tokensPacket.RefreshToken);
                    break;
                case UnauthorizedPacket unauthorizedPacket: 
                    logger.LogError(unauthorizedPacket.Description);
                    break;
            }
        }
    }
}