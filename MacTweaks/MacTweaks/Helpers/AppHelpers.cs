using System;
using System.Runtime.InteropServices;
using AppKit;
using MacTweaks.Modules.Dock;

namespace MacTweaks.Helpers;

public static class AppHelpers
{
    public static readonly uint ActualUID;

    public static readonly string ActualUsername;

    private const string LibC = "libc";
    
    static AppHelpers()
    {
        var getActualUID = new TerminalCommand("id -u $SUDO_USER").Process;

        ActualUID = uint.Parse(getActualUID.StandardOutput.ReadLine()!);

        if (IsSudoUser)
        {
            // https://unix.stackexchange.com/questions/36580/how-can-i-look-up-a-username-by-id-in-linux
            ActualUsername = new TerminalCommand($"id -nu {ActualUID}").Process.StandardOutput.ReadLine();
        }

        else
        {
            ActualUsername = Environment.UserName;
        }
    }
    
    [DllImport(LibC, EntryPoint = "getuid")]
    public static extern uint GetUID();
        
    [DllImport(LibC, EntryPoint = "setuid")]
    public static extern int SetUID(uint uid);
    
    [DllImport(LibC, EntryPoint = "geteuid")]
    public static extern uint GetEffectiveUID();
        
    // Can't transition from sudo to non-sudo
    // However, I believe you could relinquish sudo rights
    // But MacTweaks won't do that
    public static readonly bool IsSudoUser = GetUID() == 0;
    
    public static bool TryRelaunchApp(bool asSudo = false)
    {
        var macTweaks = NSRunningApplication.CurrentApplication;

        var command = asSudo ?
            $"sudo sh -c 'nohup \"{macTweaks.BundleUrl!.Path}/Contents/MacOS/{macTweaks.GetDockName().ToString()}\" > \"{ConstantHelpers.MAC_TWEAKS_LOGS_PATH}/Output.txt\" 2> \"{ConstantHelpers.MAC_TWEAKS_LOGS_PATH}/Error.txt\" &'" :
            $"sh -c 'nohup \"{macTweaks.BundleUrl!.Path}/Contents/MacOS/{macTweaks.GetDockName().ToString()}\" &'";
        
        var process = new TerminalCommand(command).Process;
                
        process.WaitForExit();

        return process.ExitCode == 0;
    }

    public static bool RelinquishSudoAccess()
    {
        // https://man7.org/linux/man-pages/man2/seteuid.2.html
        return SetUID(ActualUID) == 0;
    }
}