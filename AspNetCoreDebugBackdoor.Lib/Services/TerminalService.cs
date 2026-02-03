using AspNetCoreDebugBackdoor.Lib.Interfaces;
using AspNetCoreDebugBackdoor.Lib.Models;
using System.Diagnostics;
using System.Text;

namespace AspNetCoreDebugBackdoor.Lib.Services;

public class TerminalService : ITerminalService
{
    public async Task<TerminalResult> ExecuteAsync(TerminalRequest request)
    {
        var result = new TerminalResult();
        var sw = Stopwatch.StartNew();

        try
        {
            string fileName;
            string arguments;

            switch (request.Shell.ToLower())
            {
                case "powershell":
                case "pwsh":
                    fileName = "powershell.exe";
                    // Add $ProgressPreference silencing to avoid CLIXML progress markers in stderr
                    var silentCommand = $"$ProgressPreference = 'SilentlyContinue'; {request.Command}";
                    var bytes = Encoding.Unicode.GetBytes(silentCommand);
                    var base64 = Convert.ToBase64String(bytes);
                    arguments = $"-NoProfile -ExecutionPolicy Bypass -EncodedCommand {base64}";
                    break;
                case "cmd":
                    fileName = "cmd.exe";
                    // Simple /c usually works best if we don't over-quote the whole thing
                    arguments = $"/c {request.Command}";
                    break;
                case "bash":
                    fileName = "bash";
                    arguments = $"-c \"{request.Command.Replace("\"", "\\\"")}\"";
                    break;
                default:
                    throw new NotSupportedException($"Shell '{request.Shell}' is not supported.");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            if (!string.IsNullOrWhiteSpace(request.WorkingDirectory))
            {
                startInfo.WorkingDirectory = request.WorkingDirectory;
            }

            using var process = new Process { StartInfo = startInfo };
            
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (s, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
            process.ErrorDataReceived += (s, e) => 
            { 
                if (e.Data != null) 
                {
                    // Filter out common PowerShell noise that appears in stderr
                    if (e.Data.StartsWith("#< CLIXML") || e.Data.Contains("<Objs Version")) return;
                    if (e.Data.Contains("Preparing modules for first use")) return;
                    
                    errorBuilder.AppendLine(e.Data); 
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Timeout after 60 seconds to prevent hanging
            var completed = await Task.Run(() => process.WaitForExit(60000));
            
            if (!completed)
            {
                process.Kill(true); // Kill recursive to cleanup child processes
                result.Error = "Process timed out after 60 seconds.";
            }

            result.Output = outputBuilder.ToString();
            result.Error += errorBuilder.ToString();
            result.ExitCode = process.ExitCode;
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
            result.ExitCode = -1;
        }
        finally
        {
            sw.Stop();
            result.ExecutionTimeMs = sw.ElapsedMilliseconds;
        }

        return result;
    }
}
