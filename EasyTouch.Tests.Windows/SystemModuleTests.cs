using EasyTouch.Core.Models;
using EasyTouch.Modules;

namespace EasyTouch.Tests.Windows;

public class SystemModuleTests
{
    [Fact]
    public void Test_OsInfo()
    {
        // Act
        var result = SystemModule.GetOsInfo();
        
        // Assert
        Assert.True(result.Success, $"Get OS info failed: {result}");
        
        var successResult = Assert.IsType<SuccessResponse<OsInfoResponse>>(result);
        Assert.False(string.IsNullOrEmpty(successResult.Data.MachineName), "MachineName should not be empty");
        Assert.False(string.IsNullOrEmpty(successResult.Data.Version), "Version should not be empty");
        Assert.False(string.IsNullOrEmpty(successResult.Data.Architecture), "Architecture should not be empty");
    }

    [Fact]
    public void Test_CpuInfo()
    {
        // Act
        var result = SystemModule.GetCpuInfo();
        
        // Assert
        Assert.True(result.Success, $"Get CPU info failed: {result}");
        
        var successResult = Assert.IsType<SuccessResponse<CpuInfoResponse>>(result);
        Assert.NotNull(successResult.Data.Info);
        Assert.True(successResult.Data.UsagePercent >= 0, "UsagePercent should be non-negative");
    }

    [Fact]
    public void Test_MemoryInfo()
    {
        // Act
        var result = SystemModule.GetMemoryInfo();
        
        // Assert
        Assert.True(result.Success, $"Get memory info failed: {result}");
        
        var successResult = Assert.IsType<SuccessResponse<MemoryInfoResponse>>(result);
        Assert.True(successResult.Data.TotalPhysical > 0, "TotalPhysical memory should be greater than 0");
        Assert.True(successResult.Data.AvailablePhysical >= 0, "AvailablePhysical memory should be non-negative");
        Assert.True(successResult.Data.UsagePercent >= 0 && successResult.Data.UsagePercent <= 100, "UsagePercent should be between 0 and 100");
    }

    [Fact]
    public void Test_ListDisks()
    {
        // Act
        var result = SystemModule.ListDisks();
        
        // Assert
        Assert.True(result.Success, $"List disks failed: {result}");
        
        var successResult = Assert.IsType<SuccessResponse<DiskListResponse>>(result);
        Assert.NotNull(successResult.Data.Disks);
        Assert.True(successResult.Data.Disks.Length > 0, "Should have at least one disk");
    }

    [Fact]
    public void Test_ListProcesses()
    {
        // Arrange
        var request = new ProcessListRequest();
        
        // Act
        var result = SystemModule.ListProcesses(request);
        
        // Assert
        Assert.True(result.Success, $"List processes failed: {result}");
        
        var successResult = Assert.IsType<SuccessResponse<ProcessListResponse>>(result);
        Assert.NotNull(successResult.Data.Processes);
        Assert.True(successResult.Data.Processes.Length > 0, "Should have running processes");
    }

    [Fact]
    public void Test_ListProcesses_WithFilter()
    {
        // Arrange
        var request = new ProcessListRequest { NameFilter = "explorer" };
        
        // Act
        var result = SystemModule.ListProcesses(request);
        
        // Assert
        Assert.True(result.Success, $"List processes with filter failed: {result}");
        
        var successResult = Assert.IsType<SuccessResponse<ProcessListResponse>>(result);
        Assert.NotNull(successResult.Data.Processes);
        // 应该找到 explorer 进程
        Assert.Contains(successResult.Data.Processes, p => p.Name?.Contains("explorer") ?? false);
    }
}
