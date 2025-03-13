using System.Threading;
using System.Threading.Tasks;

namespace Interfaces
{
    public interface IRhinoBatchServices
    {
        Task<bool> OpenFileAsync(string filePath, CancellationToken ct);
        Task CloseFileAsync(CancellationToken ct);
        Task CloseAllFilesAsync(CancellationToken ct);
    }
}