using System.Text.RegularExpressions;

namespace Commons.Utils
{
    public static class ConfigNameRegex
    {
        private static readonly Regex ConfigFilePattern = new Regex(
            @"^config-(.+)\.json$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        public static bool IsValidConfigFileName(string fileName, out string projectName)
        {
            projectName = null;
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            var match = ConfigFilePattern.Match(fileName);
            if (!match.Success)
                return false;

            projectName = match.Groups[1].Value;
            return true;
        }
    }
}