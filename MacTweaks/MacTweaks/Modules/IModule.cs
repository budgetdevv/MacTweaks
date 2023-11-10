using System;
using System.Linq;
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

        public static virtual bool Enabled
        {
            get
            {
                return true;
            }
        }

        void Start();
        
        void Stop();
    }
}