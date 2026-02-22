using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using EasyTouch.Core.Models;

namespace EasyTouch.Modules;

public static class SystemModule
{
    [DllImport("kernel32.dll")]
    private static extern void GetSystemInfo(ref SYSTEM_INFO lpSystemInfo);

    [DllImport("kernel32.dll")]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [DllImport("kernel32.dll")]
    private static extern ulong GetTickCount64();

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentProcessId();

    [DllImport("kernel32.dll")]
    private static extern nint GetCurrentProcess();

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern uint GetComputerName(StringBuilder lpBuffer, ref uint nSize);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetUserName(StringBuilder lpBuffer, ref uint nSize);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetDiskFreeSpaceEx(string lpDirectoryName, out ulong lpFreeBytesAvailable, out ulong lpTotalNumberOfBytes, out ulong lpTotalNumberOfFreeBytes);

    [DllImport("kernel32.dll")]
    private static extern uint GetLogicalDrives();

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern uint GetDriveType(string lpRootPathName);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetVolumeInformation(string lpRootPathName, StringBuilder lpVolumeNameBuffer, uint nVolumeNameSize, out uint lpVolumeSerialNumber, out uint lpMaximumComponentLength, out uint lpFileSystemFlags, StringBuilder lpFileSystemNameBuffer, uint nFileSystemNameSize);

    [DllImport("psapi.dll")]
    private static extern bool GetProcessMemoryInfo(nint hProcess, ref PROCESS_MEMORY_COUNTERS counters, uint size);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern bool QueryFullProcessImageName(nint hProcess, uint dwFlags, StringBuilder lpExeName, ref uint lpdwSize);

    [DllImport("kernel32.dll")]
    private static extern nint OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(nint hObject);

    [DllImport("user32.dll")]
    private static extern bool ExitWindowsEx(uint uFlags, uint dwReason);

    [DllImport("user32.dll")]
    private static extern bool LockWorkStation();

    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEM_INFO
    {
        public ushort wProcessorArchitecture;
        public ushort wReserved;
        public uint dwPageSize;
        public nint lpMinimumApplicationAddress;
        public nint lpMaximumApplicationAddress;
        public nint dwActiveProcessorMask;
        public uint dwNumberOfProcessors;
        public uint dwProcessorType;
        public uint dwAllocationGranularity;
        public ushort wProcessorLevel;
        public ushort wProcessorRevision;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_MEMORY_COUNTERS
    {
        public uint cb;
        public uint PageFaultCount;
        public nint PeakWorkingSetSize;
        public nint WorkingSetSize;
        public nint QuotaPeakPagedPoolUsage;
        public nint QuotaPagedPoolUsage;
        public nint QuotaPeakNonPagedPoolUsage;
        public nint QuotaNonPagedPoolUsage;
        public nint PagefileUsage;
        public nint PeakPagefileUsage;
    }

    private const uint PROCESS_QUERY_INFORMATION = 0x0400;
    private const uint PROCESS_VM_READ = 0x0010;
    private const uint PROCESS_TERMINATE = 0x0001;

    private const uint DRIVE_UNKNOWN = 0;
    private const uint DRIVE_NO_ROOT_DIR = 1;
    private const uint DRIVE_REMOVABLE = 2;
    private const uint DRIVE_FIXED = 3;
    private const uint DRIVE_REMOTE = 4;
    private const uint DRIVE_CDROM = 5;
    private const uint DRIVE_RAMDISK = 6;

    private const uint EWX_LOGOFF = 0x00000000;
    private const uint EWX_SHUTDOWN = 0x00000001;
    private const uint EWX_REBOOT = 0x00000002;
    private const uint EWX_FORCE = 0x00000004;
    private const uint EWX_FORCEIFHUNG = 0x00000010;

    public static Response GetOsInfo()
    {
        try
        {
            var os = Environment.OSVersion;
            var si = new SYSTEM_INFO();
            GetSystemInfo(ref si);

            string arch = si.wProcessorArchitecture switch
            {
                0 => "x86",
                9 => "x64",
                5 => "ARM",
                12 => "ARM64",
                _ => "Unknown"
            };

            uint computerNameSize = 256;
            var computerNameSb = new StringBuilder((int)computerNameSize);
            GetComputerName(computerNameSb, ref computerNameSize);

            uint userNameSize = 256;
            var userNameSb = new StringBuilder((int)userNameSize);
            GetUserName(userNameSb, ref userNameSize);

            return new SuccessResponse<OsInfoResponse>(new OsInfoResponse
            {
                Version = $"{os.Version.Major}.{os.Version.Minor}.{os.Version.Build}",
                Architecture = arch,
                MachineName = computerNameSb.ToString(),
                UserName = userNameSb.ToString(),
                UptimeMilliseconds = (long)GetTickCount64()
            });
        }
        catch (Exception ex)
        {
            return new ErrorResponse(ex.Message);
        }
    }

    public static Response GetCpuInfo()
    {
        try
        {
            var si = new SYSTEM_INFO();
            GetSystemInfo(ref si);

            string arch = si.wProcessorArchitecture switch
            {
                0 => "x86",
                9 => "x64",
                5 => "ARM",
                12 => "ARM64",
                _ => "Unknown"
            };

            return new SuccessResponse<CpuInfoResponse>(new CpuInfoResponse
            {
                Info = new CpuInfo(
                    $"Processor ({arch})",
                    (int)si.dwNumberOfProcessors,
                    (int)si.dwNumberOfProcessors,
                    arch
                ),
                UsagePercent = 0
            });
        }
        catch (Exception ex)
        {
            return new ErrorResponse(ex.Message);
        }
    }

    public static Response GetMemoryInfo()
    {
        try
        {
            var memStatus = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
            GlobalMemoryStatusEx(ref memStatus);

            long total = (long)memStatus.ullTotalPhys;
            long available = (long)memStatus.ullAvailPhys;
            long used = total - available;
            float usagePercent = (float)(used * 100.0 / total);

            return new SuccessResponse<MemoryInfoResponse>(new MemoryInfoResponse
            {
                TotalPhysical = total,
                AvailablePhysical = available,
                UsedPhysical = used,
                UsagePercent = usagePercent
            });
        }
        catch (Exception ex)
        {
            return new ErrorResponse(ex.Message);
        }
    }

    public static Response ListDisks()
    {
        try
        {
            var disks = new List<DiskInfo>();
            uint drives = GetLogicalDrives();

            for (int i = 0; i < 26; i++)
            {
                if ((drives & (1 << i)) != 0)
                {
                    string driveLetter = $"{(char)('A' + i)}:\\";
                    
                    if (GetDiskFreeSpaceEx(driveLetter, out ulong freeBytes, out ulong totalBytes, out _))
                    {
                        uint driveType = GetDriveType(driveLetter);
                        string typeStr = driveType switch
                        {
                            DRIVE_REMOVABLE => "Removable",
                            DRIVE_FIXED => "Fixed",
                            DRIVE_REMOTE => "Network",
                            DRIVE_CDROM => "CD-ROM",
                            DRIVE_RAMDISK => "RAM Disk",
                            _ => "Unknown"
                        };

                        var volumeNameSb = new StringBuilder(256);
                        var fileSystemSb = new StringBuilder(256);
                        GetVolumeInformation(driveLetter, volumeNameSb, 256, out _, out _, out _, fileSystemSb, 256);

                        disks.Add(new DiskInfo(
                            driveLetter,
                            volumeNameSb.ToString(),
                            (long)totalBytes,
                            (long)freeBytes,
                            (long)freeBytes,
                            typeStr
                        ));
                    }
                }
            }

            return new SuccessResponse<DiskListResponse>(new DiskListResponse
            {
                Disks = disks.ToArray()
            });
        }
        catch (Exception ex)
        {
            return new ErrorResponse(ex.Message);
        }
    }

    public static Response ListProcesses(ProcessListRequest request)
    {
        try
        {
            var processes = new List<ProcessInfo>();
            var allProcesses = Process.GetProcesses();

            foreach (var proc in allProcesses)
            {
                try
                {
                    string name = proc.ProcessName;
                    
                    if (!string.IsNullOrEmpty(request.NameFilter) && 
                        !name.Contains(request.NameFilter, StringComparison.OrdinalIgnoreCase))
                        continue;

                    string? exePath = null;
                    long memory = 0;

                    try
                    {
                        exePath = proc.MainModule?.FileName;
                    }
                    catch { }

                    try
                    {
                        nint hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, (uint)proc.Id);
                        if (hProcess != 0)
                        {
                            var counters = new PROCESS_MEMORY_COUNTERS { cb = (uint)Marshal.SizeOf<PROCESS_MEMORY_COUNTERS>() };
                            if (GetProcessMemoryInfo(hProcess, ref counters, counters.cb))
                            {
                                memory = counters.WorkingSetSize;
                            }
                            CloseHandle(hProcess);
                        }
                    }
                    catch { }

                    processes.Add(new ProcessInfo((uint)proc.Id, name, exePath, memory));
                }
                catch { }
            }

            return new SuccessResponse<ProcessListResponse>(new ProcessListResponse
            {
                Processes = processes.ToArray()
            });
        }
        catch (Exception ex)
        {
            return new ErrorResponse(ex.Message);
        }
    }

    public static Response StartProcess(ProcessStartRequest request)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = request.FileName,
                Arguments = request.Arguments,
                WorkingDirectory = request.WorkingDirectory,
                UseShellExecute = true
            };

            var proc = Process.Start(psi);
            
            if (request.WaitForExit && proc != null)
            {
                proc.WaitForExit();
            }

            return new SuccessResponse<ProcessStartResponse>(new ProcessStartResponse
            {
                ProcessId = proc != null ? (uint)proc.Id : null
            });
        }
        catch (Exception ex)
        {
            return new ErrorResponse(ex.Message);
        }
    }

    public static Response KillProcess(ProcessKillRequest request)
    {
        try
        {
            if (request.ProcessId.HasValue)
            {
                var proc = Process.GetProcessById((int)request.ProcessId.Value);
                proc.Kill(request.Force);
                return new SuccessResponse();
            }
            else if (!string.IsNullOrEmpty(request.ProcessName))
            {
                var processes = Process.GetProcessesByName(request.ProcessName);
                foreach (var proc in processes)
                {
                    proc.Kill(request.Force);
                }
                return new SuccessResponse();
            }

            return new ErrorResponse("ProcessId or ProcessName must be specified");
        }
        catch (Exception ex)
        {
            return new ErrorResponse(ex.Message);
        }
    }

    public static Response Shutdown()
    {
        try
        {
            ExitWindowsEx(EWX_SHUTDOWN | EWX_FORCEIFHUNG, 0);
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse(ex.Message);
        }
    }

    public static Response Restart()
    {
        try
        {
            ExitWindowsEx(EWX_REBOOT | EWX_FORCEIFHUNG, 0);
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse(ex.Message);
        }
    }

    public static Response Logoff()
    {
        try
        {
            ExitWindowsEx(EWX_LOGOFF, 0);
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse(ex.Message);
        }
    }

    public static Response LockScreen()
    {
        try
        {
            LockWorkStation();
            return new SuccessResponse();
        }
        catch (Exception ex)
        {
            return new ErrorResponse(ex.Message);
        }
    }
}
