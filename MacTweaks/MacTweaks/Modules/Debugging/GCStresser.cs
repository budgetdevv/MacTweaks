using System;
using System.Threading.Tasks;

namespace MacTweaks.Modules.Debugging
{
    public class GCStresser: IModule
    {
        // private ThreadHelpers.MainLoopTimer Stresser;
        
        public void Start()
        {
            // Stresser = new ThreadHelpers.MainLoopTimer(TimeSpan.FromSeconds(1), x =>
            // {
            //     GC.Collect();
            // });

            Console.WriteLine("GC Stresser Launched!");

            Task.Run(async () =>
            {
                while (true)
                {
                    GC.Collect();
                    await Task.Delay(1000);
                }
            });
        }

        public void Stop()
        {
            // Stresser.Dispose();
        }
    }
}