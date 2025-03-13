using Microsoft.Extensions.DependencyInjection;
using Interfaces;

namespace RhinoInt
{
    public static class RhinoServiceConfigurator
    {
        public static IServiceCollection ConfigureRhinoServices(IServiceCollection services)
        {
            services.AddTransient<IRhinoCommOut, RhinoCommOut>();
            services.AddTransient<IRhinoUIThreadInvoker, RhinoUIThreadInvoker>();
            services.AddTransient<IRhinoBatchServices, RhinoBatchServices>();
            services.AddTransient<IRhinoPythonServices, RhinoPythonServices>();
            services.AddTransient<IRhinoGrasshopperServices, RhinoGrasshopperServices>();
            return services;
        }
    }
}