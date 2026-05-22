using OnChat.Shared.Auth;

namespace Client.Tokens;

public record TokensState;

public record TokensEmpty : TokensState;

public record UserTokens(TokenModel AccessToken, TokenModel RefreshToken) : TokensState;