using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace XrayUI.Services
{
    internal static class PresetReader
    {
        public static async Task<T> ReadJsonAsync<T>(
            string path,
            JsonTypeInfo<T> typeInfo,
            Func<T> fallback,
            string logTag)
        {
            try
            {
                var json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
                return JsonSerializer.Deserialize(json, typeInfo) ?? fallback();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{logTag}] Failed to read {path}: {ex.Message}");
                return fallback();
            }
        }
    }
}
