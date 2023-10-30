using System;
using System.Diagnostics;
using System.IO;

namespace MacTweaks.Helpers
{
    public struct RootTerminal
    {
        private readonly Process Terminal;

        private readonly StreamWriter StandardInput;
        
        private readonly StreamReader StandardOutput, StandardError;
        
        public RootTerminal(bool placeholder)
        {
            var psi = new ProcessStartInfo("/bin/bash", "-c \"dscl . list /Users | grep -v '^_'\"")
                {
                    // To hide the window, set UseShellExecute to false and RedirectStandardOutput to true
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
            
            var process = Terminal = new Process();
            process.StartInfo = psi;
            process.Start();
            
            StandardInput = process.StandardInput;
            StandardOutput = process.StandardOutput;
            StandardError = process.StandardError;
            
            Console.WriteLine(StandardOutput.ReadLine());
        }

        public void SendInput(string input)
        {
            var standardInput = StandardInput;
            
            standardInput.WriteLine(input);
            standardInput.Flush();
        }
        
        public string ReadOutput()
        {
            var standardOutput = StandardOutput;
            
            return standardOutput.ReadLine();
        }
    }
}