using System.Linq;
using System.Runtime.CompilerServices;
using MacTweaks.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace MacTweaks.Modules
{
    public interface IModule
    {
        public static readonly IModule[] Modules;

        static IModule()
        {
            Modules = AppDelegate.Services.GetServices<IModule>().ToArray();
        }
        
        public static virtual string ModuleIdentifier { get; } = null;
        
        void Start();
        
        void Stop();
    }
}