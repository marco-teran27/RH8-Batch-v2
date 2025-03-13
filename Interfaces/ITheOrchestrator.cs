using System.Threading;
using System.Threading.Tasks;

namespace Interfaces
{
    public interface ITheOrchestrator
    {
        Task<bool> RunBatchAsync(string? configPath, CancellationToken ct); // Async method
        Task<bool> RunBatch(string? configPath, CancellationToken ct); // Changed to async
    }
}