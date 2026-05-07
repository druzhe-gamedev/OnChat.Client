using System.CommandLine;
using Client.Commands.Actions;
using Client.Connection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console(
    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
).CreateLogger();

HostApplicationBuilder builder = Host.CreateApplicationBuilder();
builder.Services.AddSingleton<ChatConnection>();

IHost app = builder.Build();
await app.StartAsync();

ChatConnection connection = app.Services.GetService<ChatConnection>()!;
await connection.Connect();

Console.WriteLine("OnChat CLI Client v 1.0.0");

await ReadLoop();
return;

async Task ReadLoop()
{
    while (true)
    {
        if (!Console.KeyAvailable)
            continue;
        
        string command = Console.ReadLine()!;

        ParseResult parseResult = GenerateCommands().Parse(command);

        await parseResult.InvokeAsync();
    }    
}

RootCommand GenerateCommands()
{
    RootCommand rootCommand = 
        new("OnChat CLI Client v 1.0.0")
        {
            Subcommands =
            {
                new Command("reg", "Register account")
                {
                    Action = new RegistrationAction(connection),
                    Options =
                    {
                        new Option<string>("user")
                        {
                            Description = "Username",
                            Required = true,
                            Arity = ArgumentArity.ExactlyOne
                        },
                        new Option<string>("mail")
                        {
                            Description = "E-mail",
                            Required = true,
                            Arity = ArgumentArity.ExactlyOne
                        },
                        new Option<string>("pwd")
                        {
                            Description = "Password",
                            Required = true,
                            Arity = ArgumentArity.ExactlyOne
                        },
                        new Option<short>("age")
                        {
                            Description = "Age",
                            Required = true,
                            Arity = ArgumentArity.ExactlyOne
                        }
                    }
                },
                new Command("log", "Register account")
                {
                    Action = new AuthenticationAction(connection),
                    Options =
                    {
                        new Option<string>("login")
                        {
                            Description = "Login (username or email)",
                            Required = true,
                            Arity = ArgumentArity.ExactlyOne
                        },
                        new Option<string>("pwd")
                        {
                            Description = "Password",
                            Required = true,
                            Arity = ArgumentArity.ExactlyOne
                        }
                    }
                }
            }
        };

    return rootCommand;
}