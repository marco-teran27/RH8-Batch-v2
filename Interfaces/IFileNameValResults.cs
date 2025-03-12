using System.Collections.Generic;

namespace Interfaces
{
    public interface IFileNameValResults
    {
        bool IsValid { get; }
        IReadOnlyList<string> Messages { get; }
    }
}