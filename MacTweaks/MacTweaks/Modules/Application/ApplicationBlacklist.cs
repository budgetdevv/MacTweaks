using System.Collections.Generic;
using AppKit;
using Foundation;
using MacTweaks.Helpers;

namespace MacTweaks.Modules.Application;

public class ApplicationBlacklist: IModule
{
    private NSObject DidActivateApplicationNotification;

    private HashSet<string> Blacklist;

    public void Start()
    {
        DidActivateApplicationNotification = NSWorkspace.Notifications.ObserveDidActivateApplication(OnApplicationActivated);

        Blacklist = AppHelpers.Config.ApplicationBlacklist;
    }

    private void OnApplicationActivated(object sender, NSWorkspaceApplicationEventArgs eventArgs)
    {
        var app = eventArgs.Application;
        
        if (!Blacklist.Contains(app.BundleIdentifier))
        {
             return;
        }
        
        app.ForceTerminate();
    }
    
    public void Stop()
    {
        DidActivateApplicationNotification.Dispose();
    }
}