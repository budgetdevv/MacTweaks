using System;
using System.Diagnostics;
using AppKit;

namespace MacTweaks
{
    static class MainClass
    {
        static void Main(string[] args)
        {
            #if RELEASE
            if (Debugger.IsAttached)
            {
                // This is required, as Rider somehow ignore changes made to code
                // when ran with release build.
                throw new Exception("Don't debug in release mode!");
            }
            #endif
            
            NSApplication.Init();
            NSApplication.Main(args);
        }
    }
}