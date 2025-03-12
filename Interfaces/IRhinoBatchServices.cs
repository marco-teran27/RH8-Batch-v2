using System;

namespace Interfaces
{
    public interface IRhinoBatchServices
    {
        bool OpenFile(string filePath);
        void CloseFile();
        void CloseAllFiles();
    }
}