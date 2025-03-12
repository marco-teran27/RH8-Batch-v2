using System.Threading.Tasks;
using Interfaces;

namespace Config.Interfaces
{
    public interface IConfigParser
    {
        Task<(IConfigDataResults, IConfigValResults)> ParseConfigAsync(string configPath);
    }
}