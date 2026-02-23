using EasyTouch.Core.Models;
using EasyTouch.Modules;

namespace EasyTouch.Tests.Windows;

public class KeyboardModuleTests
{
    [Fact]
    public void Test_KeyPress_Single()
    {
        // Arrange
        var request = new KeyPressRequest { Key = "a" };
        
        // Act
        var result = KeyboardModule.Press(request);
        
        // Assert
        Assert.True(result.Success, $"Key press failed: {result}");
    }

    [Fact]
    public void Test_KeyPress_Special()
    {
        // Arrange - 测试特殊键
        var request = new KeyPressRequest { Key = "enter" };
        
        // Act
        var result = KeyboardModule.Press(request);
        
        // Assert
        Assert.True(result.Success, $"Special key press failed: {result}");
    }

    [Fact]
    public void Test_TypeText()
    {
        // Arrange
        var request = new TypeTextRequest 
        { 
            Text = "Hello World",
            Interval = 0,
            HumanLike = false
        };
        
        // Act
        var result = KeyboardModule.TypeText(request);
        
        // Assert
        Assert.True(result.Success, $"Type text failed: {result}");
    }

    [Fact]
    public void Test_KeyDown_Up()
    {
        // Act - 按下和释放 Shift 键
        var downResult = KeyboardModule.Down("shift");
        Assert.True(downResult.Success, "Key down failed");
        
        Thread.Sleep(50);
        
        var upResult = KeyboardModule.Up("shift");
        Assert.True(upResult.Success, "Key up failed");
    }

    [Fact]
    public void Test_TypeText_HumanLike()
    {
        // Arrange
        var request = new TypeTextRequest 
        { 
            Text = "Test",
            Interval = 10, // 较短的间隔
            HumanLike = true
        };
        
        // Act
        var result = KeyboardModule.TypeText(request);
        
        // Assert
        Assert.True(result.Success, $"Human-like typing failed: {result}");
    }
}
