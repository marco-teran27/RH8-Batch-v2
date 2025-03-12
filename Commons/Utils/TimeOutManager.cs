/// This updates the entire TimeOutManager class in Commons\Utils\TimeOutManager.cs
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Commons.Utils
{
    public static class TimeOutManager
    {
        public static bool RunWithTimeout(Action action, int timeOutMinutes, CancellationToken ct)
        {
            var timeoutMs = timeOutMinutes * 60 * 1000;
            var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeoutMs);

            try
            {
                Task task = Task.Run(() =>
                {
                    action();
                }, cts.Token);

                task.Wait(cts.Token); // Sync wait, offloads to thread pool
                return true;
            }
            catch (OperationCanceledException)
            {
                return false; // Timed out
            }
            catch (Exception)
            {
                throw; // Other errors propagate
            }
            finally
            {
                cts.Dispose();
            }
        }
    }
}
/// End of update for the entire TimeOutManager class in Commons\Utils\TimeOutManager.cs
