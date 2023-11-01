using System;
using System.Threading.Tasks;
using AppKit;
using Foundation;
using MacTweaks.Helpers;
using Xamarin.Essentials;

namespace MacTweaks.Modules.Energy
{
    public class LowPowerMode: IModule
    {
        private static readonly NSScreen MainScreen = NSScreen.MainScreen;

        private NSObject PowerStateDidChangeObserver;
        
        private float PreviousBrightnessLevel;

        private NSTimer BrightnessPoller;

        private void CreateAndRegisterBrightnessPoller()
        {
            var brightnessPoller = BrightnessPoller = NSTimer.CreateRepeatingTimer(TimeSpan.FromSeconds(1), timer =>
            {
                AccessibilityHelpers.GetMainDisplayBrightness(out PreviousBrightnessLevel);
            });

            NSRunLoop.Main.AddTimer(brightnessPoller, NSRunLoopMode.Common);
        }

        private void DisposeBrightnessPoller()
        {
            var brightnessPoller = BrightnessPoller;
            brightnessPoller.Invalidate();
            brightnessPoller.Dispose();
        }
        
        public void Start()
        {
            Battery.BatteryInfoChanged += BatteryInfoChanged;
            
            PowerStateDidChangeObserver = NSNotificationCenter.DefaultCenter.AddObserver(NSProcessInfo.PowerStateDidChangeNotification, PowerStateDidChange);
            
            // Create a timer that repeats every second
            CreateAndRegisterBrightnessPoller();

            _ = GCCollect();
            
            async Task GCCollect()
            {
                while (true)
                {
                    await Task.Delay(1000);
                
                    GC.Collect();
                }
            }
        }
        
        private void PowerStateDidChange(NSNotification notification)
        {
            // Yes, this event does not run on main thread
            // We need to grab PreviousBrightnessLevel on main thread
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var lowPowerModeEnabled = NSProcessInfo.ProcessInfo.LowPowerModeEnabled;

                float previousBrightnessLevel;
            
                if (lowPowerModeEnabled)
                {
                    previousBrightnessLevel = PreviousBrightnessLevel;
                    
                    DisposeBrightnessPoller();
                }

                else
                {
                    previousBrightnessLevel = -1;
                    CreateAndRegisterBrightnessPoller();
                }
            
                _ = RevertBrightness();
                
                return;

                async Task RevertBrightness()
                {
                    // ((1f / 16) * 2) + 0.0099200f is how much brightness goes up by
                    // when we transition from low power mode ( ON -> OFF ).
                    // Got the value by measuring with AccessibilityHelpers.GetBrightness().
                    // Basically, it goes up by around 2 bars, but 0.0099200f more.
                        
                    // We really really don't want to poll in low power mode, so we just
                    // modify brightness manually instead.
                    const float LOW_POWER_TRASITION_END_BRIGHTNESS_OFFSET = ((1f / 16) * 2) + 0.0099200f;
                    
                    if (!lowPowerModeEnabled)
                    {
                        AccessibilityHelpers.GetMainDisplayBrightness(out previousBrightnessLevel);
                    }
                    
                    // This delay is paramount for BOTH branches.
                    await Task.Delay(300);

                    if (!lowPowerModeEnabled)
                    {
                        if (previousBrightnessLevel + LOW_POWER_TRASITION_END_BRIGHTNESS_OFFSET <= 1)
                        {
                            // Get latest brightness level
                            AccessibilityHelpers.GetMainDisplayBrightness(out previousBrightnessLevel);
                            previousBrightnessLevel -= LOW_POWER_TRASITION_END_BRIGHTNESS_OFFSET;
                        }

                        else
                        {
                            // Unfortunately, brightness is capped at 1, so the increment by the system
                            // got truncated. We use previousBrightnessLevel from 300 ms ago as a rough
                            // estimate to restore to. Not perfect, but unnoticeable.
                        }
                    }
                
                    // Unfortunately, await continuations might run on TPL thread
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        AccessibilityHelpers.SetMainDisplayBrightness(previousBrightnessLevel);
                    });
                }     
            });
        }
        
        private const string EnableLowPowerModeOnBatteryScriptText = @"do shell script ""sudo pmset -a lowpowermode 0; sudo pmset -b lowpowermode 1"" with administrator privileges";
        
        private static readonly NSAppleScript EnableLowPowerModeOnBatteryScript = new NSAppleScript(EnableLowPowerModeOnBatteryScriptText);
        
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private void BatteryInfoChanged(object sender, BatteryInfoChangedEventArgs batteryInfoChangedEventArgs)
        {
            #if RELEASE
            if (batteryInfoChangedEventArgs.PowerSource != BatteryPowerSource.Battery)
            {
                EnableLowPowerModeOnBatteryScript.ExecuteAndReturnError(out _);
            }
            #endif
        }

        public void Stop()
        {
            Battery.BatteryInfoChanged -= BatteryInfoChanged;
            
            PowerStateDidChangeObserver.Dispose();
            
            DisposeBrightnessPoller();
        }
    }
}