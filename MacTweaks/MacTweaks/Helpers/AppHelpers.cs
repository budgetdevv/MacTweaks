using System;
using System.Collections.Generic;
using System.IO;
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
    
    private static readonly string ConfigFilePath = $"{ConstantHelpers.MAC_TWEAKS_PREFERENCES_PATH}/Config.json";
    
    public struct AppConfig: IJsonOnDeserialized
    {
        public Dictionary<string, bool> ModulesEnabledStatus;

        public AppConfig()
        {
            ModulesEnabledStatus = new Dictionary<string, bool>();
            PopulateModulesEnabledStatus();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref bool ModuleEnabledStatusGetRef(string moduleName)
        {
            return ref CollectionsMarshal.GetValueRefOrNullRef(ModulesEnabledStatus, moduleName);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref bool ModuleEnabledStatusGetRef<ModuleT>() where ModuleT: IModule
        {
            var moduleIdentifier = ModuleT.ModuleIdentifier;
            
            // TODO: Remove this when we make it mandatory to declare identifier
            // Sadly the JIT does not specialize where ModuleT: class
            if (moduleIdentifier != null)
            {
                return ref ModuleEnabledStatusGetRef(ModuleT.ModuleIdentifier);
            }

            else
            {
                return ref Unsafe.NullRef<bool>();
            }
        }

        public readonly ref struct ModuleStatusEnabledHandle<ModuleT> where ModuleT: IModule
        {
            private readonly ref bool Enabled;

            private readonly ref AppConfig Config;
            
            public ModuleStatusEnabledHandle(ref AppConfig config)
            {
                Enabled = ref config.ModuleEnabledStatusGetRef<ModuleT>();
                Config = ref config;
            }

            public void SaveChanges()
            {
                Config.Save();
            }
            
            public void Dispose()
            {
                SaveChanges();
            }
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

        public void Save()
        {
            File.WriteAllText(ConfigFilePath, JsonSerializer.Serialize(this, JOpts));
        }
    }
    
    private static readonly JsonSerializerOptions JOpts = new JsonSerializerOptions
    {
        IncludeFields = true,
        WriteIndented = true
    };

    public static AppConfig Config;
    
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
        
        if (!Directory.Exists(ConstantHelpers.MAC_TWEAKS_PREFERENCES_PATH))
        {
            Directory.CreateDirectory(ConstantHelpers.MAC_TWEAKS_PREFERENCES_PATH);

            goto FileDoesNotExist;
        }

        AppConfig config;

        if (File.Exists(ConfigFilePath))
        {
            config = JsonSerializer.Deserialize<AppConfig>(ConfigFilePath, JOpts);

            goto SetConfig;
        }
        
        FileDoesNotExist:
        config = new AppConfig();

        config.Save();
        
        SetConfig:
        Config = config;
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