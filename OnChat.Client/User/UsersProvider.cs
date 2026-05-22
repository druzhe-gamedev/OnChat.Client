using System.Collections.Concurrent;
using OnChat.Shared.Users;

namespace Client.User;

public class UsersProvider
{
    public readonly ConcurrentDictionary<Guid, UserModel> Users = [];
}