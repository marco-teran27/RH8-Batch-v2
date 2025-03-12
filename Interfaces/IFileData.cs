using System.Collections.Generic;

namespace Interfaces
{
    public interface IFileData
    {
        IReadOnlyList<string> MatchedFiles { get; }
    }
}