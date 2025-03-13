using System.Threading;
using System.Threading.Tasks;

namespace Interfaces
{
    public interface IRhinoGrasshopperServices
    {
        Task<bool> RunScriptAsync(CancellationToken ct);
    }
}