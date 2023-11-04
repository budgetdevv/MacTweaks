using System;

namespace MacTweaks.Helpers
{
    public static class ConstantHelpers
    {
        public const string APP_NAME = "MacTweaks",
                            FINDER_APP_NAME = "Finder",
                            SECURITY_AGENT_NAME = "SecurityAgent",
                            APP_ICON_PATH = "Contents/Resources/AppIcon.icns";
        
        public static readonly string MAC_TWEAKS_LOGS_PATH = $"{Environment.GetFolderPath(Environment.SpecialFolder.Personal)}/Library/Logs/{APP_NAME}";
    }
}