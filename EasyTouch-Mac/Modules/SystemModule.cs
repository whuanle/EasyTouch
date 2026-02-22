using System.Diagnostics;
using EasyTouch.Core.Models;

namespace EasyTouch.Modules;

public static class SystemModule
{
    public static Response GetOsInfo()
    {
        try
        {
            var swVersion = RunCommand("sw_vers", "");
            var productName = "";
            var productVersion = "";
            var buildVersion = "";
            
            foreach (var line in swVersion.Split('\n'))
            {
                if (line.StartsWith("ProductName:"))
                    productName = line.Substring(12).Trim();
                else if (line.StartsWith("ProductVersion:"))
                    productVersion = line.Substring(15).Trim();
                else if (line.StartsWith("BuildVersion:"))
                    buildVersion = line.Substring(13).Trim();
            }
            
            var arch = RunCommand("uname", "-m").Trim();
            
            return new SuccessResponse<OsInfoResponse>(new OsInfoResponse
            {
                Platform = $"macOS ({productName})",
                Version = $"{productVersion} ({buildVersion})",
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
            var cpuName = RunCommand("sysctl", "-n machdep.cpu.brand_string").Trim();
            var coreCount = Environment.ProcessorCount;
            
            // Get CPU usage using top
            var output = RunCommand("top", "-l 1 -n 0 | grep \"CPU usage\"");
            double usage = 0;
            var usageMatch = System.Text.RegularExpressions.Regex.Match(output, @"([\d.]+)% idle");
            if (usageMatch.Success && double.TryParse(usageMatch.Groups[1].Value, out var idle))
            {
                usage = 100 - idle;
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
            var vmStats = RunCommand("vm_stat", "");
            long pageSize = 4096; // Default page size
            
            if (long.TryParse(RunCommand("sysctl", "-n hw.pagesize").Trim(), out var ps))
            {
                pageSize = ps;
            }
            
            long freePages = 0, activePages = 0, inactivePages = 0, wiredPages = 0;
            
            foreach (var line in vmStats.Split('\n'))
            {
                if (line.StartsWith("Pages free:"))
                    long.TryParse(System.Text.RegularExpressions.Regex.Match(line, "\\d+").Value, out freePages);
                else if (line.StartsWith("Pages active:"))
                    long.TryParse(System.Text.RegularExpressions.Regex.Match(line, "\\d+").Value, out activePages);
                else if (line.StartsWith("Pages inactive:"))
                    long.TryParse(System.Text.RegularExpressions.Regex.Match(line, "\\d+").Value, out inactivePages);
                else if (line.StartsWith("Pages wired down:"))
                    long.TryParse(System.Text.RegularExpressions.Regex.Match(line, "\\d+").Value, out wiredPages);
            }
            
            var totalMemory = 0L;
            if (long.TryParse(RunCommand("sysctl", "-n hw.memsize").Trim(), out var tm))
            {
                totalMemory = tm;
            }
            
            var usedMemory = (activePages + inactivePages + wiredPages) * pageSize;
            var freeMemory = freePages * pageSize;
            var usagePercent = totalMemory > 0 ? (double)usedMemory / totalMemory * 100 : 0;
            
            return new SuccessResponse<MemoryInfoResponse>(new MemoryInfoResponse
            {
                Total = totalMemory,
                Available = freeMemory,
                Used = usedMemory,
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
            var output = RunCommand("df", "-h");
            var lines = output.Split('\n').Skip(1);
            
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 9 && parts[0].StartsWith("/dev/"))
                {
                    disks.Add(new DiskInfo
                    {
                        Name = parts[0].Replace("/dev/", ""),
                        MountPoint = parts[8],
                        FileSystem = parts[0],
                        Total = ParseSize(parts[1]),
                        Used = ParseSize(parts[2]),
                        Free = ParseSize(parts[3])
                    });
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
            var output = RunCommand("ps", "-eo pid,comm,pcpu,pmem -r");
            var lines = output.Split('\n').Skip(1);
            
            foreach (var line in lines)
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
                            Memory = (long)(mem * 1024 * 1024),
                            CpuUsage = cpu
                        });
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
            // Use built-in screen lock
            RunCommand("osascript", "-e 'tell application \"System Events\" to keystroke \"q\" using {control down, command down}'");
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
        else if (size.EndsWith("i"))
        {
            // Handle Mi, Gi, Ti
            if (size.EndsWith("Mi"))
            {
                multiplier = 1024 * 1024;
                size = size[..^2];
            }
            else if (size.EndsWith("Gi"))
            {
                multiplier = 1024 * 1024 * 1024;
                size = size[..^2];
            }
        }
        
        if (double.TryParse(size, out var value))
        {
            return (long)(value * multiplier);
        }
        
        return 0;
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
