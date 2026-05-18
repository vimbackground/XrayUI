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
                return await ReadRequiredJsonAsync(path, typeInfo, logTag).ConfigureAwait(false);
            }
            catch
            {
                return fallback();
            }
        }

        public static async Task<T> ReadRequiredJsonAsync<T>(
            string path,
            JsonTypeInfo<T> typeInfo,
            string logTag)
        {
            try
            {
                var json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
                return JsonSerializer.Deserialize(json, typeInfo)
                    ?? throw new JsonException("Preset JSON cannot be null.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{logTag}] Failed to read {path}: {ex.Message}");
                throw new InvalidDataException(
                    $"Failed to import preset file '{Path.GetFileName(path)}'. Please check that it contains valid JSON.",
                    ex);
            }
        }
    }
}
