using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using AppKit;
using MacTweaks.Modules;
using MacTweaks.Modules.Dock;

namespace MacTweaks.Helpers;

public static class AppHelpers
{
    public static readonly uint ActualUID;

    public static readonly string ActualUsername;

    private const string LibC = "libc";
    
    public struct AppConfig: IJsonOnDeserialized
    {
        public Dictionary<string, bool> ModulesEnabledStatus;

        public AppConfig()
        {
            ModulesEnabledStatus = new Dictionary<string, bool>();
            PopulateModulesEnabledStatus();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ModuleEnabled(string moduleName)
        {
            var dict = ModulesEnabledStatus;

            // It should throw if a given moduleName doesn't exist.
            // This is why we don't use TryGetValue()
            return dict[moduleName];
        }

        public void OnDeserialized()
        {
            PopulateModulesEnabledStatus();
        }
        
        private void PopulateModulesEnabledStatus()
        {
            var propName = nameof(IModule.ModuleIdentifier);

            var bf = (BindingFlags) (-1);

            var dict = ModulesEnabledStatus;
            
            foreach (var module in IModule.Modules)
            {
                var prop = module.GetType().GetProperty(propName, bf);

                if (prop != null)
                {
                    var moduleName = Unsafe.As<string>(prop.GetValue(null));
                    
                    // Add won't work if there is already existing value
                    var isNew = dict.TryAdd(moduleName, true);
                }
            }
        }
    }
    
    private static readonly JsonSerializerOptions JOpts = new JsonSerializerOptions
    {
        IncludeFields = true,
        WriteIndented = true
    };
    
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

        // var zzz = JsonSerializer.Serialize(new AppConfig(), JOpts);
        //
        // Console.WriteLine(zzz);
        //
        // var x = JsonSerializer.Deserialize<AppConfig>(zzz, JOpts);
        //
        // Console.WriteLine(JsonSerializer.Serialize(x, JOpts));
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