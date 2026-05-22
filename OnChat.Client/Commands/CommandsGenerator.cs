using System.CommandLine;
using Client.Commands.Actions;
using Client.Connection;
using Client.Keys;
using Client.Tokens;
using Client.User;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Client.Commands;

public class CommandsGenerator(
    IServiceProvider serviceProvider,
    ChatConnection connection,
    TokensService tokensService,
    UsersProvider usersProvider,
    KeysVault keysVault)
{
    public RootCommand GenerateCommands()
    {
        RootCommand rootCommand = 
            new("OnChat CLI Client v 1.0.0")
            {
                Subcommands =
                {
                    new Command("reg", "Register account")
                    {
                        Action = new RegistrationAction(connection, serviceProvider.GetService<ILogger<RegistrationAction>>()!),
                        Options =
                        {
                            new Option<string>("-u")
                            {
                                Description = "Username",
                                Required = true,
                                Arity = ArgumentArity.ExactlyOne
                            },
                            new Option<string>("-m")
                            {
                                Description = "E-mail",
                                Required = true,
                                Arity = ArgumentArity.ExactlyOne
                            },
                            new Option<string>("-p")
                            {
                                Description = "Password",
                                Required = true,
                                Arity = ArgumentArity.ExactlyOne
                            },
                            new Option<short>("-a")
                            {
                                Description = "Age",
                                Required = true,
                                Arity = ArgumentArity.ExactlyOne
                            }
                        }
                    },
                    new Command("log", "Log in")
                    {
                        Action = new AuthenticationAction(connection, tokensService, serviceProvider.GetService<ILogger<AuthenticationAction>>()!),
                        Options =
                        {
                            new Option<string>("-l")
                            {
                                Description = "Login (username or email)",
                                Required = true,
                                Arity = ArgumentArity.ExactlyOne
                            },
                            new Option<string>("-p")
                            {
                                Description = "Password",
                                Required = true,
                                Arity = ArgumentArity.ExactlyOne
                            }
                        }
                    },
                    new Command("msg", "Send message")
                    {
                        Action = new SendMessageAction(connection, tokensService, keysVault, usersProvider, serviceProvider.GetService<ILogger<SendMessageAction>>()!),
                        Options =
                        {
                            new Option<Guid>("-r")
                            {
                                Description = "Receiver guid",
                                Required = true,
                                Arity = ArgumentArity.ExactlyOne
                            },
                            new Option<string>("-m")
                            {
                                Description = "Message",
                                Required = true,
                                Arity = ArgumentArity.ExactlyOne
                            }
                        }
                    },
                    new Command("users", "Fetch users info")
                    {
                        Action = new GetUsersModelsAction(connection, usersProvider, serviceProvider.GetService<ILogger<GetUsersModelsAction>>()!),
                    },
                    new Command("key-gen", "Generate keys pair for an account")
                    {
                        Action = new GenerateKeysAction(connection, tokensService, keysVault, serviceProvider.GetService<ILogger<GenerateKeysAction>>()!)
                    },
                    new Command("load-msgs", "Load messages from user")
                    {
                        Action = new LoadMessagesAction(connection, tokensService, usersProvider, keysVault, serviceProvider.GetService<ILogger<LoadMessagesAction>>()!),
                        Options =
                        {
                            new Option<Guid>("-i")
                            {
                                Description = "Chat participant id",
                                Required = true,
                                Arity = ArgumentArity.ExactlyOne
                            },
                            new Option<int>("-p")
                            {
                                Description = "Page",
                                Required = true,
                                Arity = ArgumentArity.ExactlyOne
                            },
                            new Option<int>("-q")
                            {
                                Description = "Messages per page",
                                Required = true,
                                Arity = ArgumentArity.ExactlyOne
                            }
                        }
                    }
                }
            };

        return rootCommand;
    }
}