using System;

namespace MacTweaks.Helpers
{
    public class ConstantHelpers
    {
        public const string APP_NAME = "MacTweaks",
                            FINDER_APP_NAME = "Finder";
        
        public static readonly string MAC_TWEAKS_LOGS_PATH = $"{Environment.GetFolderPath(Environment.SpecialFolder.Personal)}/Library/Logs/{APP_NAME}";
    }
}