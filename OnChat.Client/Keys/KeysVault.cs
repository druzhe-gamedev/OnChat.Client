using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Client.Keys;

public class KeysVault(ILogger<KeysVault> logger)
{
    private static readonly string RoamingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    private static readonly string KeysFolderPath = Path.Combine(RoamingDirectory, "OnChat", "secret");

    private readonly ConcurrentDictionary<Guid, KeysRecord> _keys = [];

    static KeysVault()
    {
        Directory.CreateDirectory(KeysFolderPath);
    }
    
    public async Task FetchKeys()
    {
        string[] keysFiles = Directory.GetFiles(KeysFolderPath, "*.json");

        StringBuilder sb = new($"Reading {KeysFolderPath}");
        
        if (keysFiles.Length == 0)
        {
            sb.Append("No key pairs saved yet");
            logger.LogInformation(sb.ToString());
            return;
        }
        
        try
        {
            foreach (string file in keysFiles)
            {
                string json = await File.ReadAllTextAsync(file);

                string guidStr = file[(file.LastIndexOf('\\') + 1)..file.IndexOf(".json", StringComparison.Ordinal)];
                Guid guid = Guid.Parse(guidStr);
                _keys[guid] = JsonSerializer.Deserialize<KeysRecord>(json)!;
                
                sb.Append($"\nLoad key pairs for {guid}");
            }
            
            logger.LogInformation(sb.ToString());
        }
        catch (Exception e)
        {
            logger.LogError("Error occured while reading keys {EMessage}", e.Message);
        }
    }

    public bool TryGetKeysRecord(Guid userId, out KeysRecord? keys)
    {
        keys = null;

        return _keys.TryGetValue(userId, out keys);
    }

    public async Task AddOrUpdateKeysRecord(Guid userId, KeysRecord keys)
    {
        string keysFile = Path.Combine(KeysFolderPath, userId + ".json");
        string json = JsonSerializer.Serialize(keys);

        _keys.AddOrUpdate(userId, keys, (_, _) => keys);
        await File.WriteAllTextAsync(keysFile, json);
    }
}