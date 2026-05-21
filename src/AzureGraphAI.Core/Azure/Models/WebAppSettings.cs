using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AzureGraphAI.Core.Azure.Models;

public sealed class AppSettings
{
    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
    public string? Id { get; set; }

    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string? Name { get; set; }

    [JsonProperty("properties", NullValueHandling = NullValueHandling.Ignore)]
    public AppSettingProperties Properties { get; set; } = new();
}

public sealed class AppSettingProperties
{
    [JsonExtensionData]
    public Dictionary<string, JToken> AdditionalProperties { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public string GetSettingValue(params string[] possibleSettingNames)
    {
        foreach (var settingName in possibleSettingNames)
        {
            if (AdditionalProperties.TryGetValue(settingName, out var value) && value.Type != JTokenType.Null)
                return value.ToString();
        }

        return string.Empty;
    }

    public IReadOnlyList<string> GetSettingValues(IEnumerable<string> possibleSettingNames)
    {
        var values = new List<string>();
        foreach (var settingName in possibleSettingNames)
        {
            if (AdditionalProperties.TryGetValue(settingName, out var value) && value.Type != JTokenType.Null)
            {
                var raw = value.ToString();
                if (!string.IsNullOrWhiteSpace(raw))
                    values.Add(raw);
            }
        }

        return values.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    public string GetKeyVaultName(params string[] possibleSettingNames)
        => KeyVault.GetKeyVaultName(GetSettingValue(possibleSettingNames));
}

public sealed class ConnectionStrings
{
    [JsonProperty("properties")]
    [JsonConverter(typeof(ConnectionStringPropertiesConverter))]
    public Dictionary<string, ConnectionString> Properties { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ConnectionString
{
    public string? Value { get; set; }

    public ConnectionToService? ConnectionToService { get; set; }
}

public sealed class ConnectionStringPropertiesConverter : JsonConverter<Dictionary<string, ConnectionString>>
{
    public override Dictionary<string, ConnectionString> ReadJson(JsonReader reader, Type objectType, Dictionary<string, ConnectionString>? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var result = new Dictionary<string, ConnectionString>(StringComparer.OrdinalIgnoreCase);
        var obj = JObject.Load(reader);

        foreach (var property in obj)
        {
            var rawValue = property.Value?["value"]?.ToString();
            if (string.IsNullOrWhiteSpace(rawValue))
                continue;

            var connection = ConnectionToService.Build(rawValue);
            result[property.Key] = new ConnectionString
            {
                Value = connection.ConnectionString,
                ConnectionToService = connection
            };
        }

        return result;
    }

    public override void WriteJson(JsonWriter writer, Dictionary<string, ConnectionString>? value, JsonSerializer serializer)
        => serializer.Serialize(writer, value);
}

public enum AzureConnectedServiceType
{
    Unknown,
    StorageAccount,
    RedisCache,
    SqlServer,
    ServiceBus,
    KeyVault
}

public sealed class ConnectionToService
{
    public string ConnectionString { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public AzureConnectedServiceType Type { get; private set; } = AzureConnectedServiceType.Unknown;

    public static ConnectionToService Build(string fullConnectionString)
    {
        var connection = new ConnectionToService
        {
            ConnectionString = RedactSecret(fullConnectionString)
        };

        connection.Initialize();
        return connection;
    }

    private void Initialize()
    {
        if ((FindStartIndex(ConnectionString, ["Server=", "server =", "Data Source=", "Data Source ="]) > -1)
            && FindStartIndex(ConnectionString, ["password=", "password ="]) > -1)
        {
            Type = AzureConnectedServiceType.SqlServer;
            Name = ExtractName(["Server=", "Server =", "Data Source=", "Data Source ="], ".");
            if (Name.StartsWith("tcp:", StringComparison.OrdinalIgnoreCase))
                Name = Name[4..];
            return;
        }

        if (ConnectionString.StartsWith("DefaultEndpointsProtocol", StringComparison.OrdinalIgnoreCase))
        {
            Type = AzureConnectedServiceType.StorageAccount;
            Name = ExtractName(["AccountName=", "AccountName ="]);
            return;
        }

        if (ConnectionString.StartsWith("Endpoint=sb", StringComparison.OrdinalIgnoreCase))
        {
            Type = AzureConnectedServiceType.ServiceBus;
            Name = ExtractName(["Endpoint=", "Endpoint ="], ".servicebus.windows.net");
            if (Name.StartsWith("sb://", StringComparison.OrdinalIgnoreCase))
                Name = Name[5..];
            return;
        }

        if (ConnectionString.Contains("redis.cache.windows.net", StringComparison.OrdinalIgnoreCase))
        {
            Type = AzureConnectedServiceType.RedisCache;
            Name = ExtractName([], ".redis.cache.windows.net", string.Empty);
            return;
        }

        if (ConnectionString.Contains("vault.azure.net", StringComparison.OrdinalIgnoreCase))
        {
            Type = AzureConnectedServiceType.KeyVault;
            Name = ExtractName("https://", ".vault.azure.net");
        }
    }

    public static string RedactSecret(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        var index = FindStartIndex(value, ["password=", "password =", "AccountKey=", "AccountKey =", "SharedAccessKey=", "SharedAccessKey ="]);
        if (index < 0)
            return value;

        var trueStart = value.IndexOf("=", index, StringComparison.Ordinal) + 1;
        var endIndex = FindEndIndex(value, trueStart);
        if (endIndex == -1)
            endIndex = FindEndIndex(value, trueStart, ",");
        if (endIndex == -1)
            endIndex = value.Length;

        return value.Remove(trueStart, endIndex - trueStart).Insert(trueStart, "REDACTED");
    }

    private string ExtractName(string key, string terminatingString)
    {
        var startIndex = ConnectionString.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (startIndex == -1)
            return string.Empty;

        startIndex += key.Length;
        var endIndex = ConnectionString.IndexOf(terminatingString, startIndex, StringComparison.OrdinalIgnoreCase);
        if (endIndex == -1)
            endIndex = ConnectionString.Length;

        return TrimSlashes(ConnectionString[startIndex..endIndex].Trim());
    }

    private string ExtractName(IEnumerable<string> possibleKeys, string terminatingValue = ";", string trueStartChar = "=")
    {
        var startIndex = FindStartIndex(ConnectionString, possibleKeys);
        if (startIndex < 0)
            return string.Empty;

        var trueStart = startIndex;
        if (!string.IsNullOrWhiteSpace(trueStartChar))
            trueStart = ConnectionString.IndexOf(trueStartChar, startIndex, StringComparison.Ordinal) + 1;

        var endIndex = FindEndIndex(ConnectionString, trueStart, terminatingValue);
        if (endIndex == -1)
            endIndex = ConnectionString.Length;

        return TrimSlashes(ConnectionString[trueStart..endIndex].Trim());
    }

    private static string TrimSlashes(string value)
        => value.TrimEnd('/', '\\');

    private static int FindEndIndex(string value, int trueStart, string terminatingValue = ";")
        => value.IndexOf(terminatingValue, trueStart, StringComparison.OrdinalIgnoreCase);

    private static int FindStartIndex(string value, IEnumerable<string> possibleKeys)
    {
        var keys = possibleKeys.ToList();
        if (keys.Count == 0)
            return 0;

        foreach (var possibleKey in keys)
        {
            var index = value.IndexOf(possibleKey, StringComparison.OrdinalIgnoreCase);
            if (index != -1)
                return index;
        }

        return -1;
    }
}
