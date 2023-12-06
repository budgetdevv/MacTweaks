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
    
    private static readonly string ConfigFilePath = $"{ConstantHelpers.MAC_TWEAKS_PREFERENCES_PATH}/Config.json",
                                   BackupConfigPath = $"{ConstantHelpers.MAC_TWEAKS_PREFERENCES_PATH}/ConfigBackup.json";
    
    public struct AppConfig: IJsonOnDeserialized
    {
        public Dictionary<string, bool> ModulesEnabledStatus;

        public HashSet<string> RedQuitWhitelist, ApplicationBlacklist;

        public AppConfig(): this(isCreate: false)
        {
            // JsonSerializer.Deserialize calls this ctor
        }

        // Allow isCreate to be constant-folded
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AppConfig(bool isCreate)
        {
            // This constructor is called, even when deserialization happens
            // Avoid double calling. ( OnDeserialized() calls it too )
            if (isCreate)
            {
                EnsureFieldsArePopulated(isCreate: true);
            }
        }

        // Allow isCreate to be constant-folded
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureFieldsArePopulated(bool isCreate)
        {
            var isNotCreate = !isCreate;
            
            var modulesEnabledStatus = ModulesEnabledStatus;
            ModulesEnabledStatus = (isNotCreate && modulesEnabledStatus != null) ? modulesEnabledStatus : new Dictionary<string, bool>();

            var redQuitWhitelist = RedQuitWhitelist;
            RedQuitWhitelist = (isNotCreate && redQuitWhitelist != null) ? redQuitWhitelist : new HashSet<string>()
            {
                ConstantHelpers.FINDER_BUNDLE_ID // We don't want to terminate Finder...it will cause desktop to go KABOOM!
            };
            
            var applicationBlacklist = ApplicationBlacklist;
            ApplicationBlacklist = (isNotCreate && applicationBlacklist != null) ? applicationBlacklist : new HashSet<string>();
            
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
            EnsureFieldsArePopulated(isCreate: false);
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
            try
            {
                config = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(ConfigFilePath), JOpts);

                goto SetConfig;
            }

            catch
            {
                try
                {
                    File.Move(ConfigFilePath, BackupConfigPath);
                }

                catch
                {
                    // Ignored
                }
            }
        }   
        
        FileDoesNotExist:
        config = new AppConfig(isCreate: true);
        
        SetConfig:
        Config = config;
        
        // Unconditionally save the config. For new configs, this is a given to ensure that a config exists.
        // For existing configs, it enables us to update it with new fields specific to newer iterations of the app.
        config.Save();
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