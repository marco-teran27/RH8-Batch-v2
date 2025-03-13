using Rhino;
using Rhino.Commands;
using Interfaces;
using DInjection;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RhinoInt
{
    [Rhino.Commands.CommandStyle(Rhino.Commands.Style.ScriptRunner)]
    public class BatchProcessorStart : Command
    {
        private readonly ITheOrchestrator _orchestrator;
        private static IServiceProvider _serviceProvider;

        public BatchProcessorStart()
        {
            _serviceProvider ??= InitializeServices();
            _orchestrator = _serviceProvider.GetService<ITheOrchestrator>();
        }

        internal BatchProcessorStart(ITheOrchestrator orchestrator)
        {
            _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        }

        private static IServiceProvider InitializeServices()
        {
            var services = new ServiceCollection();
            // Register non-Rhino services
            ServiceConfigurator.ConfigureServices(services);
            // Register Rhino-specific services
            RhinoServiceConfigurator.ConfigureRhinoServices(services);
            return services.BuildServiceProvider();
        }

        public override string EnglishName => "BatchProcessor";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                RhinoApp.WriteLine($"[DEBUG] BatchProcessorStart.RunCommand started on Thread {Thread.CurrentThread.ManagedThreadId} at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");

                // Run the async operation and wait without deadlocking
                var task = Task.Run(() => RunBatchAsync(doc, mode));
                while (!task.IsCompleted)
                {
                    RhinoApp.Wait(); // Allow the main thread to process messages
                    Thread.Sleep(100); // Brief sleep to prevent tight looping
                }
                bool success = task.Result;

                RhinoApp.WriteLine($"[DEBUG] BatchProcessorStart.RunCommand ended with success: {success} on Thread {Thread.CurrentThread.ManagedThreadId} at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
                return success ? Result.Success : Result.Failure;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"BatchProcessor failed: {ex.Message} (Stack: {ex.StackTrace})");
                return Result.Failure;
            }
        }

        private async Task<bool> RunBatchAsync(RhinoDoc doc, RunMode mode)
        {
            return await _orchestrator.RunBatchAsync(null, CancellationToken.None);
        }
    }
}