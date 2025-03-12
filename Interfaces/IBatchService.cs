using System.Threading;
using System.Threading.Tasks;

namespace Interfaces
{
    public interface IBatchService
    {
        Task RunBatchAsync(CancellationToken ct);
        void CloseAllFiles();
    }
}