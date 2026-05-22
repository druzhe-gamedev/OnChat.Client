using System.CommandLine;
using Client.Commands;
using Client.Connection;
using Client.Keys;
using Client.Tokens;
using Client.User;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console(
    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
).CreateLogger();

HostApplicationBuilder builder = Host.CreateApplicationBuilder();

builder.Services.AddSerilog();
builder.Services.AddSingleton<KeysVault>();
builder.Services.AddSingleton<ChatConnection>();
builder.Services.AddSingleton<UsersProvider>();
builder.Services.AddSingleton<TokensService>();
builder.Services.AddSingleton<CommandsGenerator>();

IHost app = builder.Build();

ChatConnection connection = app.Services.GetRequiredService<ChatConnection>();
await connection.Connect();

CommandsGenerator commandsGenerator = app.Services.GetRequiredService<CommandsGenerator>();
await app.Services.GetRequiredService<KeysVault>().FetchKeys();

await app.StartAsync();

Log.Information("OnChat CLI Client v 1.0.0");
await ReadLoop();
return;

async Task ReadLoop()
{
    while (true)
    {
        if (!Console.KeyAvailable)
            continue;
        
        string command = Console.ReadLine()!;

        ParseResult parseResult = commandsGenerator.GenerateCommands().Parse(command);

        await parseResult.InvokeAsync();
    }    
}
