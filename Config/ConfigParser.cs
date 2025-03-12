using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Config.Interfaces;
using Config.Models;
using Config.Validation;
using Interfaces;

namespace Config
{
    public class ConfigParser : IConfigParser
    {
        private readonly ICommonsDataService _commonsDataService;
        private readonly IRhinoCommOut _rhinoCommOut;

        public ConfigParser(ICommonsDataService commonsDataService, IRhinoCommOut rhinoCommOut)
        {
            _commonsDataService = commonsDataService ?? throw new ArgumentNullException(nameof(commonsDataService));
            _rhinoCommOut = rhinoCommOut ?? throw new ArgumentNullException(nameof(rhinoCommOut));
        }

        public async Task<(IConfigDataResults, IConfigValResults)> ParseConfigAsync(string configPath)
        {
            try
            {
                string jsonText = await File.ReadAllTextAsync(configPath);
                using JsonDocument doc = JsonDocument.Parse(jsonText);
                JsonElement root = doc.RootElement;

                // Extract raw minutes value and adjust JSON if needed
                string rawMinutes = null;
                string adjustedJson = jsonText;
                if (root.TryGetProperty("timeout_settings", out JsonElement timeoutSettings) &&
                    timeoutSettings.TryGetProperty("minutes", out JsonElement minutesElement))
                {
                    rawMinutes = minutesElement.ToString();
                    if (!int.TryParse(rawMinutes, out _))
                    {
                        // Replace non-integer minutes with 0 to ensure deserialization succeeds
                        string original = $"\"minutes\": {minutesElement.GetRawText()}";
                        string replacement = "\"minutes\": 0";
                        adjustedJson = jsonText.Replace(original, replacement);
                    }
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                };

                ConfigDataResults configData = JsonSerializer.Deserialize<ConfigDataResults>(adjustedJson, options);
                ConfigValResults configVal = new ConfigValResults(configData, configPath, rawMinutes);

                return (configData, configVal);
            }
            catch (Exception ex)
            {
                _rhinoCommOut.ShowError($"CONFIG PARSING FAILED: {ex.Message}");
                return (null, null);
            }
        }
    }
}