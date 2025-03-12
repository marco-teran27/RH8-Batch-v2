using Rhino;
using Rhino.Commands;
using Interfaces;
using DInjection;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;

namespace RhinoInt
{
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
            ServiceConfigurator.ConfigureServices(services);
            RhinoServiceConfigurator.ConfigureRhinoServices(services);
            return services.BuildServiceProvider();
        }

        public override string EnglishName => "BatchProcessor";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                bool success = _orchestrator.RunBatch(null, CancellationToken.None);
                return success ? Result.Success : Result.Failure;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"BatchProcessor failed: {ex.Message}");
                return Result.Failure;
            }
        }
    }
}