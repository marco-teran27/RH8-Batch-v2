using System.Threading;
using Interfaces;

namespace Interfaces
{
    public interface ICommonsDataService
    {
        void UpdateFromConfig(IConfigDataResults config, IConfigValResults valResults);
        void UpdateFromFileDir(IFileNameList fileList, IFileNameValResults valResults);
    }
}