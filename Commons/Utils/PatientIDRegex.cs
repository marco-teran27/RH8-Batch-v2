using System.Text.RegularExpressions;

namespace Commons.Utils
{
    public static class PatientIDRegex
    {
        public static readonly Regex Pattern = new Regex(
            @"^(\d{6}[LR])-([SR]\d{5})$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );
    }
}