using System;
using AppKit;
using MacTweaks.Modules.Dock;

namespace MacTweaks.Helpers;

public static class AppHelpers
{
    public static bool TryRelaunchApp(bool asSudo = false)
    {
        var macTweaks = NSRunningApplication.CurrentApplication;

        var command = asSudo ?
            $"sudo sh -c 'nohup \"{macTweaks.BundleUrl!.Path}/Contents/MacOS/{macTweaks.GetDockName().ToString()}\" > \"{ConstantHelpers.MAC_TWEAKS_LOGS_PATH}/Output.txt\" 2> \"{ConstantHelpers.MAC_TWEAKS_LOGS_PATH}/Error.txt\" &'" :
            $"sh -c 'nohup \"{macTweaks.BundleUrl!.Path}/Contents/MacOS/{macTweaks.GetDockName().ToString()}\" &'";

        Console.WriteLine(command);
        
        var process = new TerminalCommand(command).Process;
                
        process.WaitForExit();

        return process.ExitCode == 0;
    }
}