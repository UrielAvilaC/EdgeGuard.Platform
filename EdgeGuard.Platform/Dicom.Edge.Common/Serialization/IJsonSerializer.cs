using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dicom.Edge.Common.Serialization
{
    /// <summary>
    /// JSON serialization service with consistent settings.
    /// </summary>
    public interface IJsonSerializer
    {
        string Serialize<T>(T obj);
        T? Deserialize<T>(string json);
    }

    /// <summary>
    /// Default JSON serializer implementation using System.Text.Json.
    /// </summary>
    public class JsonSerializer : IJsonSerializer
    {
        private readonly JsonSerializerOptions _options;

        public JsonSerializer()
        {
            _options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters =
                {
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                }
            };
        }

        public JsonSerializer(JsonSerializerOptions options)
        {
            _options = options;
        }

        public string Serialize<T>(T obj)
        {
            return System.Text.Json.JsonSerializer.Serialize(obj, _options);
        }

        public T? Deserialize<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return default;

            return System.Text.Json.JsonSerializer.Deserialize<T>(json, _options);
        }
    }
}
