using System;
using AppKit;
using Foundation;

namespace MacTweaks.Helpers
{
    public static class ThreadHelpers
    {
        private static readonly NSRunningApplication CurrentApplication = NSRunningApplication.CurrentApplication;

        public static void InvokeOnMainThread(Action action)
        {
            CurrentApplication.InvokeOnMainThread(action);
        }

        public readonly struct MainLoopTimer: IDisposable
        {
            private static readonly NSRunLoop MainLoop = NSRunLoop.Main;
            
            private readonly NSTimer Timer;

            // Prevent GC from collecting it.
            // TODO: Find out if this is necessary, or does NSTimer hold onto a reference.
            private readonly Action<NSTimer> Action;
            
            public MainLoopTimer(TimeSpan interval, Action<NSTimer> action, NSRunLoopMode mode = NSRunLoopMode.Common)
            {
                var timer = Timer = NSTimer.CreateRepeatingTimer(interval, Action = action);
                
                MainLoop.AddTimer(timer, mode);
            }

            public void Dispose()
            {
                var timer = Timer;
                
                timer.Invalidate();
                timer.Dispose();
            }
        }
    }
}