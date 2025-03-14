using System;
using System.Threading;
using System.Threading.Tasks;

namespace Commons.Utils
{
    public static class TimeOutManager
    {
        public static async Task<bool> RunWithTimeoutAsync(Func<CancellationToken, Task> actionAsync, int timeOutMinutes, CancellationToken externalCt)
        {
            var timeoutMs = timeOutMinutes * 60 * 1000;
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(externalCt);
            cts.CancelAfter(timeoutMs);

            try
            {
                var task = Task.Run(() => actionAsync(cts.Token), cts.Token);
                var delayTask = Task.Delay(timeoutMs, cts.Token);

                var completedTask = await Task.WhenAny(task, delayTask);
                if (completedTask == task)
                {
                    await task; // Ensure exceptions propagate
                    return true; // Completed within timeout
                }
                else
                {
                    cts.Cancel(); // Signal cancellation
                    return false; // Timed out
                }
            }
            catch (OperationCanceledException)
            {
                return false; // Timed out or canceled
            }
            catch (Exception)
            {
                throw; // Propagate other errors
            }
        }
    }
}