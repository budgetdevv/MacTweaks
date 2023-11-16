using System;

namespace MacTweaks.Helpers
{
    public static class ConstantHelpers
    {
        public const string APP_NAME = "MacTweaks",
                            FINDER_APP_NAME = "Finder",
                            FINDER_BUNDLE_ID = "com.apple.finder",
                            SECURITY_AGENT_NAME = "SecurityAgent",
                            APP_ICON_PATH = "Contents/Resources/AppIcon.icns",
                            LOCKDOWN_BROWSER_BUNDLE_ID = "com.Respondus.LockDownBrowser";
        
        
        public static readonly string LIBRARY_PATH = $"{Environment.GetFolderPath(Environment.SpecialFolder.Personal)}/Library",
                                      MAC_TWEAKS_LOGS_PATH = $"{LIBRARY_PATH}/Logs/{APP_NAME}",
                                      MAC_TWEAKS_PREFERENCES_PATH = $"{LIBRARY_PATH}/Preferences/{APP_NAME}";
    }
}