namespace Interfaces
{
    public interface IRhinoUIThreadInvoker
    {
        void InvokeOnUIThread(Action action);
    }
}