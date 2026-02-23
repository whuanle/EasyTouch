using EasyTouch.Core.Models;
using EasyTouch.Modules;

namespace EasyTouch.Tests.Windows;

public class ScreenModuleTests
{
    [Fact]
    public void Test_Screenshot()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_screenshot_{Guid.NewGuid()}.png");
        var request = new ScreenshotRequest { OutputPath = tempFile };
        
        try
        {
            // Act
            var result = ScreenModule.Screenshot(request);
            
            // Assert
            Assert.True(result.Success, $"Screenshot failed: {result}");
            Assert.True(File.Exists(tempFile), "Screenshot file was not created");
            
            var fileInfo = new FileInfo(tempFile);
            Assert.True(fileInfo.Length > 0, "Screenshot file is empty");
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void Test_Screenshot_WithCoordinates()
    {
        // Arrange - 截图特定区域
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_screenshot_partial_{Guid.NewGuid()}.png");
        var request = new ScreenshotRequest 
        { 
            OutputPath = tempFile,
            X = 0,
            Y = 0,
            Width = 100,
            Height = 100
        };
        
        try
        {
            // Act
            var result = ScreenModule.Screenshot(request);
            
            // Assert
            Assert.True(result.Success, $"Partial screenshot failed: {result}");
            Assert.True(File.Exists(tempFile), "Screenshot file was not created");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void Test_PixelColor()
    {
        // Arrange
        var request = new PixelColorRequest { X = 100, Y = 100 };
        
        // Act
        var result = ScreenModule.GetPixelColor(request);
        
        // Assert
        Assert.True(result.Success, $"Get pixel color failed: {result}");
        
        var successResult = Assert.IsType<SuccessResponse<PixelColorResponse>>(result);
        Assert.True(successResult.Data.R >= 0 && successResult.Data.R <= 255, "R should be 0-255");
        Assert.True(successResult.Data.G >= 0 && successResult.Data.G <= 255, "G should be 0-255");
        Assert.True(successResult.Data.B >= 0 && successResult.Data.B <= 255, "B should be 0-255");
    }

    [Fact]
    public void Test_ListScreens()
    {
        // Act
        var result = ScreenModule.ListScreens();
        
        // Assert
        Assert.True(result.Success, $"List screens failed: {result}");
        
        var successResult = Assert.IsType<SuccessResponse<ScreenListResponse>>(result);
        Assert.NotNull(successResult.Data.Screens);
        Assert.True(successResult.Data.Screens.Length > 0, "Should have at least one screen");
    }
}
