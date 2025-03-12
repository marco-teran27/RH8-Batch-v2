using System.Text.RegularExpressions;

namespace Commons.Utils
{
    public static class RhinoNameRegex
    {
        /// <summary>
        /// This regex enforces the naming pattern for .3dm files and uniqueIds in the batch pipeline.
        /// 
        /// Pattern Explanation:
        ///  - ^(\d{6}[LR])\d*?: Start with 6 digits plus 'L' or 'R', followed by zero or more digits (optional, no hyphen for files, hyphen for uniqueIds)
        ///    Example: 300051L2 or 300051L- for uniqueIds
        ///  - (?:-)?(.+): Optionally a hyphen, then any keyword or descriptor
        ///  - -v[\w\d]*?: A hyphen, 'v', and zero or more word characters or digits (version, ignored for matching, optional)
        ///  - -([SR]\d{5}): A hyphen, then 'S' or 'R' followed by 5 digits
        ///  - \.3dm$: Finally, ensure the file extension is .3dm (case-insensitive)
        /// 
        /// RegexOptions:
        ///  - Compiled for performance
        ///  - IgnoreCase so we match .3dm in any letter casing
        /// </summary>
        public static readonly Regex RhinoFilePattern = new Regex(
            @"^(\d{6}[LR])\d*?(?:-)?(.+)-v[\w\d]*?-([SR]\d{5})\.3dm$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );
    }
}