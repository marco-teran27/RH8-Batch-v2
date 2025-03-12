using Microsoft.Extensions.DependencyInjection;
using Interfaces;
using RhinoInt;
using Config.Interfaces; // Ensure this is included if needed elsewhere

namespace RhinoInt
{
    public static class RhinoServiceConfigurator
    {
        public static IServiceCollection ConfigureRhinoServices(IServiceCollection services)
        {
            services.AddTransient<IRhinoCommOut, RhinoCommOut>();
            services.AddTransient<IRhinoBatchServices>(provider =>
                new RhinoBatchServices(provider.GetService<IRhinoCommOut>()));
            services.AddTransient<IRhinoPythonServices, RhinoPythonServices>(); // No parameters needed
            services.AddTransient<IRhinoGrasshopperServices, RhinoGrasshopperServices>(); // Added for Grasshopper
            return services;
        }
    }
}