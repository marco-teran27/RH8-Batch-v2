using Rhino;
using Interfaces;
using System;

namespace RhinoInt
{
    public class RhinoUIThreadInvoker : IRhinoUIThreadInvoker
    {
        public void InvokeOnUIThread(Action action)
        {
            RhinoApp.InvokeOnUiThread(action);
        }
    }
}