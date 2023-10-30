using AppKit;

namespace MacTweaks
{
    static class MainClass
    {
        private static void Main(string[] args)
        {
            #if RELEASE
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // This is required, as Rider somehow ignore changes made to code
                // when ran with release build.
                throw new System.Exception("Don't debug in release mode!");
            }
            #endif
            
            NSApplication.Init();
            NSApplication.Main(args);
        }
    }
}