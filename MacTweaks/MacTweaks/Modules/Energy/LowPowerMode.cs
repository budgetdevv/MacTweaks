using System.Threading.Tasks;
using AppKit;
using MacTweaks.Helpers;
using Xamarin.Essentials;

namespace MacTweaks.Modules.Energy
{
    public class LowPowerMode: IModule
    {
        private static readonly NSScreen MainScreen = NSScreen.MainScreen;
        
        public void Start()
        {
            Battery.BatteryInfoChanged += BatteryInfoChanged;
        }

        private void BatteryInfoChanged(object sender, BatteryInfoChangedEventArgs batteryInfoChangedEventArgs)
        {
            // Fortunately, brightness level isn't altered before the invocation of this method
            AccessibilityHelpers.GetMainDisplayBrightness(out var brightnessLevel);

            _ = RevertBrightness();
            
            async Task RevertBrightness()
            {
                // Anything lower, and the brightness will be overriden
                await Task.Delay(300);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    AccessibilityHelpers.SetMainDisplayBrightness(brightnessLevel);
                });
            }
        }

        public void Stop()
        {
            Battery.BatteryInfoChanged -= BatteryInfoChanged;
        }
    }
}