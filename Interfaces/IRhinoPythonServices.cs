using System.Threading;

namespace Interfaces
{
    public interface IRhinoPythonServices
    {
        bool RunScript(CancellationToken ct);
    }
}