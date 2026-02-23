using EasyTouch.Core.Models;
using EasyTouch.Modules;

namespace EasyTouch.Tests.Windows;

public class ClipboardModuleTests
{
    [Fact]
    public void Test_Clipboard_SetAndGet()
    {
        // Arrange
        var testText = $"Test Clipboard {Guid.NewGuid()}";
        var setRequest = new ClipboardSetTextRequest { Text = testText };
        
        // Act - 设置剪贴板
        var setResult = ClipboardModule.SetText(setRequest);
        Assert.True(setResult.Success, $"Set clipboard failed: {setResult}");
        
        // Act - 读取剪贴板
        var getRequest = new ClipboardGetTextRequest();
        var getResult = ClipboardModule.GetText(getRequest);
        
        // Assert
        Assert.True(getResult.Success, $"Get clipboard failed: {getResult}");
        
        var successResult = Assert.IsType<SuccessResponse<ClipboardTextResponse>>(getResult);
        Assert.Equal(testText, successResult.Data.Text);
    }

    [Fact]
    public void Test_Clipboard_Clear()
    {
        // Arrange - 先设置一些内容
        ClipboardModule.SetText(new ClipboardSetTextRequest { Text = "To be cleared" });
        
        // Act - 清空剪贴板
        var clearResult = ClipboardModule.Clear(new ClipboardClearRequest());
        
        // Assert
        Assert.True(clearResult.Success, $"Clear clipboard failed: {clearResult}");
    }

    [Fact]
    public void Test_Clipboard_Empty()
    {
        // Arrange - 清空剪贴板
        ClipboardModule.Clear(new ClipboardClearRequest());
        
        // Act
        var result = ClipboardModule.GetText(new ClipboardGetTextRequest());
        
        // Assert - 空剪贴板应该返回空字符串或失败
        Assert.True(result.Success, "Getting empty clipboard should succeed");
        
        if (result is SuccessResponse<ClipboardTextResponse> success)
        {
            Assert.True(string.IsNullOrEmpty(success.Data.Text) || success.Data.Text == "", 
                "Empty clipboard should return empty string");
        }
    }

    [Fact]
    public void Test_Clipboard_SpecialCharacters()
    {
        // Arrange - 测试特殊字符
        var testText = "Special: !@#$%^&*()_+-=[]{}|;':\",./<>?\n\t中文";
        var setRequest = new ClipboardSetTextRequest { Text = testText };
        
        // Act
        ClipboardModule.SetText(setRequest);
        var result = ClipboardModule.GetText(new ClipboardGetTextRequest());
        
        // Assert
        Assert.True(result.Success);
        var successResult = Assert.IsType<SuccessResponse<ClipboardTextResponse>>(result);
        Assert.Equal(testText, successResult.Data.Text);
    }
}
