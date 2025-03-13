using Microsoft.Extensions.DependencyInjection;
using Config;
using Config.Interfaces;
using Interfaces;
using Core;
using Core.Batch;
using FileDir;
using Commons.LogComm;

namespace DInjection
{
    public static class ServiceConfigurator
    {
        public static IServiceCollection ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<ICommonsDataService, CommonsDataService>();
            services.AddTransient<IConfigSelUI, ConfigSelUI>();
            services.AddTransient<IConfigParser>(provider =>
                new ConfigParser(
                    provider.GetService<ICommonsDataService>(),
                    provider.GetService<IRhinoCommOut>()));
            services.AddTransient<IFileDirParser, FileDirParser>();
            services.AddTransient<FileNameValComm>();
            services.AddTransient<ConfigValComm>();
            services.AddTransient<ITheOrchestrator, TheOrchestrator>();
            services.AddTransient<IBatchService>(provider =>
                new BatchService(
                    provider.GetService<IRhinoCommOut>(),
                    provider.GetService<IRhinoBatchServices>(),
                    provider.GetService<IRhinoPythonServices>(),
                    provider.GetService<IRhinoGrasshopperServices>()));
            return services;
        }
    }
}