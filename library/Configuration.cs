using System.Text.Json;
using Azure.Core;
using Azure.Storage.Blobs;

namespace ServcoX.SimpleSharedCache
{
    public class Configuration
    {
        public JsonSerializerOptions SerializerOptions { get; set; } = new()
        {
            IgnoreNullValues = true,
            // After upgrade: DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        };

        public BlobClientOptions BlobClientOptions { get; set; } = new()
        {
            Retry =
            {
                Mode = RetryMode.Exponential,
                Delay = TimeSpan.FromSeconds(0.5),
                MaxRetries = 4,
            },
        };

        public String ContainerName { get; set; } = "cache";

        public Configuration UseContainerName(String containerName)
        {
            ContainerName = containerName;
            return this;
        }
    }
}