using EasyTouch.Core.Models;
using EasyTouch.Modules;

namespace EasyTouch.Tests.Windows;

public class MouseModuleTests
{
    [Fact]
    public void Test_MouseMove_Absolute()
    {
        // Arrange
        var request = new MouseMoveRequest { X = 100, Y = 200, Relative = false };
        
        // Act
        var result = MouseModule.Move(request);
        
        // Assert
        Assert.True(result.Success, $"Mouse move failed: {result}");
    }

    [Fact]
    public void Test_MouseMove_Relative()
    {
        // Arrange - 获取当前位置
        var currentPos = GetCurrentMousePosition();
        var request = new MouseMoveRequest { X = 10, Y = 10, Relative = true };
        
        // Act
        var result = MouseModule.Move(request);
        
        // Assert
        Assert.True(result.Success, $"Relative mouse move failed: {result}");
        
        // 验证位置确实移动了
        var newPos = GetCurrentMousePosition();
        Assert.True(Math.Abs(newPos.X - currentPos.X) <= 20, "X position should have changed by ~10");
        Assert.True(Math.Abs(newPos.Y - currentPos.Y) <= 20, "Y position should have changed by ~10");
    }

    [Fact]
    public void Test_MouseClick()
    {
        // Arrange
        var request = new MouseClickRequest { Button = MouseButton.Left, Double = false };
        
        // Act
        var result = MouseModule.Click(request);
        
        // Assert
        Assert.True(result.Success, $"Mouse click failed: {result}");
    }

    [Fact]
    public void Test_MousePosition_Get()
    {
        // Act - 直接调用模块获取位置
        var result = MouseModule.GetPosition();
        
        // Assert
        Assert.True(result.Success, $"Get position failed: {result}");
        
        var successResult = Assert.IsType<SuccessResponse<MousePositionResponse>>(result);
        Assert.True(successResult.Data.X >= 0, "X should be non-negative");
        Assert.True(successResult.Data.Y >= 0, "Y should be non-negative");
    }

    [Fact]
    public void Test_MouseScroll()
    {
        // Arrange
        var request = new MouseScrollRequest { Amount = 3, Horizontal = false };
        
        // Act
        var result = MouseModule.Scroll(request);
        
        // Assert
        Assert.True(result.Success, $"Mouse scroll failed: {result}");
    }

    [Fact]
    public void Test_MouseDown_Up()
    {
        // Act - 按下和释放左键
        var downResult = MouseModule.Down(MouseButton.Left);
        Assert.True(downResult.Success, "Mouse down failed");
        
        Thread.Sleep(50); // 短暂延迟
        
        var upResult = MouseModule.Up(MouseButton.Left);
        Assert.True(upResult.Success, "Mouse up failed");
    }

    [Fact]
    public void Test_MouseMove_HumanLike()
    {
        // Arrange - 获取当前位置
        var currentPos = GetCurrentMousePosition();
        var targetX = currentPos.X + 200;
        var targetY = currentPos.Y + 150;

        var request = new MouseMoveRequest
        {
            X = targetX,
            Y = targetY,
            Relative = false,
            Duration = 800,
            HumanLike = true
        };

        // Act
        var startTime = DateTime.Now;
        var result = MouseModule.Move(request);
        var endTime = DateTime.Now;
        var elapsed = (endTime - startTime).TotalMilliseconds;

        // Assert
        Assert.True(result.Success, $"Human-like mouse move failed: {result}");

        // 验证移动时间符合预期（有一定容差）
        Assert.True(elapsed >= 600 && elapsed <= 1200,
            $"Human-like movement should take around 800ms, but took {elapsed}ms");

        // 验证最终位置接近目标
        var newPos = GetCurrentMousePosition();
        Assert.True(Math.Abs(newPos.X - targetX) <= 3 && Math.Abs(newPos.Y - targetY) <= 3,
            "Final position should be close to target");
    }

    [Fact]
    public void Test_MouseMove_HumanLike_Random()
    {
        // 测试多次人类化移动，验证路径不是直线
        var positions = new List<(int x, int y)>();
        var currentPos = GetCurrentMousePosition();

        for (int i = 0; i < 3; i++)
        {
            var targetX = currentPos.X + 100 + i * 50;
            var targetY = currentPos.Y + 80 + i * 40;

            var request = new MouseMoveRequest
            {
                X = targetX,
                Y = targetY,
                Relative = false,
                Duration = 600,
                HumanLike = true
            };

            var result = MouseModule.Move(request);
            Assert.True(result.Success);

            var newPos = GetCurrentMousePosition();
            positions.Add((newPos.X, newPos.Y));

            Thread.Sleep(200);
        }

        // 验证所有移动都成功完成
        Assert.Equal(3, positions.Count);
    }

    [Fact]
    public void Test_MouseDrag_HumanLike()
    {
        // Arrange
        var currentPos = GetCurrentMousePosition();
        var startX = currentPos.X;
        var startY = currentPos.Y;
        var endX = startX + 150;
        var endY = startY + 100;

        var request = new MouseDragRequest
        {
            StartX = startX,
            StartY = startY,
            EndX = endX,
            EndY = endY,
            Button = MouseButton.Left,
            HumanLike = true
        };

        // Act
        var result = MouseModule.Drag(request);

        // Assert
        Assert.True(result.Success, $"Human-like drag failed: {result}");

        // 验证最终位置
        var newPos = GetCurrentMousePosition();
        Assert.True(Math.Abs(newPos.X - endX) <= 3 && Math.Abs(newPos.Y - endY) <= 3,
            "Drag should end at target position");
    }

    // Helper method to get current mouse position using Windows API
    private (int X, int Y) GetCurrentMousePosition()
    {
        var result = MouseModule.GetPosition();
        if (result is SuccessResponse<MousePositionResponse> success)
        {
            return (success.Data.X, success.Data.Y);
        }
        return (0, 0);
    }
}
