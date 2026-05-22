namespace Client.Keys;

public record KeysRecord(Guid UserId, byte[] PublicKey, byte[] PrivateKey);