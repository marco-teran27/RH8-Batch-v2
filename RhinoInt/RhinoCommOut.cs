using Rhino;
using Interfaces;

namespace RhinoInt
{
    public class RhinoCommOut : IRhinoCommOut
    {
        public void ShowMessage(string message)
        {
            RhinoApp.WriteLine(message);
        }

        public void ShowError(string error)
        {
            RhinoApp.WriteLine($"Error: {error}");
        }
    }
}