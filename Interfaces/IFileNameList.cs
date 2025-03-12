using System.Collections.Generic;

namespace Interfaces
{
    public interface IFileNameList
    {
        IReadOnlyList<string> MatchedFiles { get; }
    }
}