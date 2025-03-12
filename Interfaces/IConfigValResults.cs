using System.Collections.Generic;

namespace Interfaces
{
    public interface IConfigValResults // Added public
    {
        bool IsValid { get; }
        IReadOnlyList<(string Name, bool IsValid, IReadOnlyList<string> Messages)> Results { get; }
    }
}