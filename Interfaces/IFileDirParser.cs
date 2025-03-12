using System.Threading.Tasks;

namespace Interfaces
{
    public interface IFileDirParser
    {
        Task<(IFileNameList Data, IFileNameValResults Val)> ParseFileDirAsync(string fileDir, IReadOnlyList<string> uniqueIds, IConfigDataResults config);
    }
}