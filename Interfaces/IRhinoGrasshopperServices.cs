namespace Interfaces
{
    public interface IRhinoGrasshopperServices
    {
        bool RunScript(System.Threading.CancellationToken ct);
    }
}