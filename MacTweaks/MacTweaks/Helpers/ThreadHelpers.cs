using System;
using System.Runtime.CompilerServices;
using AppKit;
using Foundation;

namespace MacTweaks.Helpers
{
    public static class ThreadHelpers
    {
        private static readonly NSRunningApplication CurrentApplication = NSRunningApplication.CurrentApplication;

        // Allow tryExecuteInline to be constant folded, eliminating a branch if it is false
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InvokeOnMainThread(Action action, bool tryExecuteInline = false)
        {
            if (tryExecuteInline && IsMainThread())
            {
                action();
            }

            else
            {
                CurrentApplication.BeginInvokeOnMainThread(action);
            }
        }
        
        // Allow tryExecuteInline to be constant folded, eliminating a branch if it is false
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InvokeOnMainThreadBlocking(Action action, bool tryExecuteInline = false)
        {
            if (tryExecuteInline && IsMainThread())
            {
                action();
            }

            else
            {
                CurrentApplication.InvokeOnMainThread(action);
            }
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
        
        // https://learn.microsoft.com/en-us/dotnet/api/system.threading.thread.managedthreadid?view=net-7.0
        // "The value of the ManagedThreadId property does not vary over time, even if unmanaged code that hosts the common language runtime implements the thread as a fiber."
        private static readonly int MainThreadID;

        static ThreadHelpers()
        {
            int threadID;
            
            // We do this check, since we don't want to
            // block InvokeOnMainThreadBlocking if we are
            // running on main thread already. Doing so will
            // cause a dead lock
            if (NSThread.Current.IsMainThread)
            {
                threadID = Environment.CurrentManagedThreadId;
            }

            else
            {
                var threadIDBox = new StrongBox<int>();
            
                InvokeOnMainThreadBlocking(() =>
                {
                    threadIDBox.Value = Environment.CurrentManagedThreadId;
                });

                threadID = threadIDBox.Value;
            }

            MainThreadID = threadID;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMainThread()
        {
            return Environment.CurrentManagedThreadId == MainThreadID;
        }
    }
}