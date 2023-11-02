using System.Diagnostics;

namespace MacTweaks.Helpers
{
    public readonly struct TerminalCommand
    {
        public readonly Process Process;
        
        public TerminalCommand(string command)
        {
            var psi = new ProcessStartInfo("/bin/zsh", $"-c \"{command}\"")
            {
                // To hide the window, set UseShellExecute to false and RedirectStandardOutput to true
                UseShellExecute = false,
                RedirectStandardOutput = true, 
                RedirectStandardInput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            var process = Process = new Process();
            process.StartInfo = psi;
            process.Start();
        }
    }
}