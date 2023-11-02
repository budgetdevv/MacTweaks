using System;
using AppKit;

namespace MacTweaks.Helpers
{
    public static class ThreadHelpers
    {
        private static readonly NSRunningApplication CurrentApplication = NSRunningApplication.CurrentApplication;
        
        public static void InvokeOnMainThread(Action action)
        {
            CurrentApplication.InvokeOnMainThread(action);
        }
    }
}