using System;
using CoreFoundation;
using MacTweaks.Helpers;

namespace MacTweaks.Modules.Energy.PlatformBattery
{
    public static partial class Battery
    {
        static CFRunLoopSource powerSourceNotification;
        
        static void StartBatteryListeners()
        {
            powerSourceNotification = IOKit.CreatePowerSourceNotification(PowerSourceNotification);
            CFRunLoop.Current.AddSource(powerSourceNotification, CFRunLoop.ModeDefault);
        }

        static void StopBatteryListeners()
        {
            if (powerSourceNotification != null)
            {
                CFRunLoop.Current.RemoveSource(powerSourceNotification, CFRunLoop.ModeDefault);
                powerSourceNotification = null;
            }
        }

        static void PowerSourceNotification()
            => ThreadHelpers.InvokeOnMainThread(OnBatteryInfoChanged);

        static double PlatformChargeLevel => IOKit.GetInternalBatteryChargeLevel();

        static BatteryState PlatformState => IOKit.GetInternalBatteryState();

        static BatteryPowerSource PlatformPowerSource => IOKit.GetProvidingPowerSource();

        static void StartEnergySaverListeners()
        {
        }

        static void StopEnergySaverListeners()
        {
        }

        static EnergySaverStatus PlatformEnergySaverStatus => EnergySaverStatus.Off;
    }
}
