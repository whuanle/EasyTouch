using System.Diagnostics;
using EasyTouch.Core.Models;

namespace EasyTouch.Modules;

public static class SystemModule
{
    public static Response GetOsInfo()
    {
        try
        {
            var osName = RunCommand("uname", "-s").Trim();
            var osVersion = RunCommand("uname", "-r").Trim();
            var arch = RunCommand("uname", "-m").Trim();
            
            // Try to get distribution info
            string distro = "";
            if (File.Exists("/etc/os-release"))
            {
                var osRelease = File.ReadAllText("/etc/os-release");
                var nameMatch = System.Text.RegularExpressions.Regex.Match(osRelease, "PRETTY_NAME=\"([^\"]+)\"");
                if (nameMatch.Success)
                {
                    distro = nameMatch.Groups[1].Value;
                }
            }
            
            return new SuccessResponse<OsInfoResponse>(new OsInfoResponse
            {
                Platform = $"Linux ({distro ?? osName})",
                Version = osVersion,
                Architecture = arch,
                MachineName = Environment.MachineName,
                UserName = Environment.UserName
            });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Get OS info failed: {ex.Message}");
        }
    }

    public static Response GetCpuInfo()
    {
        try
        {
            string cpuName = "Unknown";
            int coreCount = Environment.ProcessorCount;
            
            if (File.Exists("/proc/cpuinfo"))
            {
                var cpuinfo = File.ReadAllText("/proc/cpuinfo");
                var modelMatch = System.Text.RegularExpressions.Regex.Match(cpuinfo, @"model\s+name\s*:\s*(.+)", System.Text.RegularExpressions.RegexOptions.Multiline);
                if (modelMatch.Success)
                {
                    cpuName = modelMatch.Groups[1].Value.Trim();
                }
            }
            
            // Get CPU usage using top
            var output = RunCommand("top", "-bn1 | grep \"Cpu(s)\"");
            double usage = 0;
            var usageMatch = System.Text.RegularExpressions.Regex.Match(output, @"([\d.]+)\s*%us");
            if (usageMatch.Success && double.TryParse(usageMatch.Groups[1].Value, out var us))
            {
                usage = us;
            }
            
            return new SuccessResponse<CpuInfoResponse>(new CpuInfoResponse
            {
                Name = cpuName,
                CoreCount = coreCount,
                ThreadCount = coreCount,
                Usage = usage
            });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Get CPU info failed: {ex.Message}");
        }
    }

    public static Response GetMemoryInfo()
    {
        try
        {
            if (!File.Exists("/proc/meminfo"))
            {
                return new ErrorResponse("Cannot read memory info");
            }
            
            var meminfo = File.ReadAllText("/proc/meminfo");
            
            var totalMatch = System.Text.RegularExpressions.Regex.Match(meminfo, @"MemTotal:\s+(\d+)");
            var availableMatch = System.Text.RegularExpressions.Regex.Match(meminfo, @"MemAvailable:\s+(\d+)");
            
            long total = 0, available = 0;
            
            if (totalMatch.Success)
                long.TryParse(totalMatch.Groups[1].Value, out total);
            if (availableMatch.Success)
                long.TryParse(availableMatch.Groups[1].Value, out available);
            
            // Convert from KB to bytes
            total *= 1024;
            available *= 1024;
            var used = total - available;
            var usagePercent = total > 0 ? (double)used / total * 100 : 0;
            
            return new SuccessResponse<MemoryInfoResponse>(new MemoryInfoResponse
            {
                Total = total,
                Available = available,
                Used = used,
                UsagePercent = usagePercent
            });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Get memory info failed: {ex.Message}");
        }
    }

    public static Response ListDisks()
    {
        try
        {
            var disks = new List<DiskInfo>();
            var output = RunCommand("df", "-h --output=source,size,used,avail,target");
            var lines = output.Split('\n').Skip(1); // Skip header
            
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                var parts = line.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 5 && !parts[4].StartsWith("/sys") && !parts[4].StartsWith("/proc") && !parts[4].StartsWith("/dev"))
                {
                    var fs = parts[0];
                    if (fs.StartsWith("/dev/"))
                    {
                        disks.Add(new DiskInfo
                        {
                            Name = fs.Replace("/dev/", ""),
                            MountPoint = parts[4],
                            FileSystem = fs,
                            Total = ParseSize(parts[1]),
                            Used = ParseSize(parts[2]),
                            Free = ParseSize(parts[3])
                        });
                    }
                }
            }
            
            return new SuccessResponse<DiskListResponse>(new DiskListResponse { Disks = disks });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"List disks failed: {ex.Message}");
        }
    }

    public static Response ListProcesses(ProcessListRequest request)
    {
        try
        {
            var processes = new List<ProcessInfo>();
            var psi = new ProcessStartInfo
            {
                FileName = "ps",
                Arguments = "-eo pid,comm,pcpu,pmem --no-headers",
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            
            using var process = Process.Start(psi);
            if (process != null)
            {
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                
                foreach (var line in output.Split('\n'))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    
                    var parts = line.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 4)
                    {
                        var name = parts[1];
                        
                        if (!string.IsNullOrEmpty(request.NameFilter) &&
                            !name.Contains(request.NameFilter, StringComparison.OrdinalIgnoreCase))
                            continue;
                        
                        if (uint.TryParse(parts[0], out var pid) &&
                            double.TryParse(parts[2], out var cpu) &&
                            double.TryParse(parts[3], out var mem))
                        {
                            processes.Add(new ProcessInfo
                            {
                                Id = pid,
                                Name = name,
                                Title = name,
                                Memory = (long)(mem * 1024 * 1024), // Approximate
                                CpuUsage = cpu
                            });
                        }
                    }
                }
            }
            
            return new SuccessResponse<ProcessListResponse>(new ProcessListResponse { Processes = processes });
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"List processes failed: {ex.Message}");
        }
    }

    public static Response LockScreen()
    {
        try
        {
            // Try different screen lockers
            if (CommandExists("gnome-screensaver-command"))
            {
                RunCommand("gnome-screensaver-command", "-l");
            }
            else if (CommandExists("xscreensaver-command"))
            {
                RunCommand("xscreensaver-command", "-lock");
            }
            else if (CommandExists("i3lock"))
            {
                RunCommand("i3lock", "");
            }
            else if (CommandExists("loginctl"))
            {
                RunCommand("loginctl", "lock-session");
            }
            else
            {
                return new ErrorResponse("No screen locker found");
            }
            
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Lock screen failed: {ex.Message}");
        }
    }

    private static long ParseSize(string size)
    {
        if (string.IsNullOrEmpty(size)) return 0;
        
        size = size.Trim();
        var multiplier = 1L;
        
        if (size.EndsWith("K"))
        {
            multiplier = 1024;
            size = size[..^1];
        }
        else if (size.EndsWith("M"))
        {
            multiplier = 1024 * 1024;
            size = size[..^1];
        }
        else if (size.EndsWith("G"))
        {
            multiplier = 1024 * 1024 * 1024;
            size = size[..^1];
        }
        else if (size.EndsWith("T"))
        {
            multiplier = 1024L * 1024 * 1024 * 1024;
            size = size[..^1];
        }
        
        if (double.TryParse(size, out var value))
        {
            return (long)(value * multiplier);
        }
        
        return 0;
    }

    private static bool CommandExists(string command)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "which",
                Arguments = command,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            
            using var process = Process.Start(psi);
            process?.WaitForExit();
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static string RunCommand(string command, string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        
        using var process = Process.Start(psi);
        if (process == null)
            throw new InvalidOperationException($"Failed to start {command}");
        
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        
        if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
            throw new Exception($"{command} failed: {error}");
        
        return output;
    }
}
