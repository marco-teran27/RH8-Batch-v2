using System.Threading;
using System.Threading.Tasks;

namespace Interfaces
{
    public interface IRhinoPythonServices
    {
        Task<bool> RunScriptAsync(CancellationToken ct);
    }
}