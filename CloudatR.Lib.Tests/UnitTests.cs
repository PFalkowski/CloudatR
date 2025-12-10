using CloudatR.Lib.Abstractions;
using FluentAssertions;

namespace CloudatR.Lib.Tests;

public class UnitTests
{
    [Fact]
    public void Unit_Value_IsNotNull()
    {
        // Act & Assert
        Unit.Value.Should().NotBeNull();
    }

    [Fact]
    public void Unit_Value_IsAlwaysEqual()
    {
        // Act
        var value1 = Unit.Value;
        var value2 = Unit.Value;

        // Assert
        value1.Should().Be(value2);
        value1.Equals(value2).Should().BeTrue();
    }

    [Fact]
    public void Unit_Equals_ReturnsTrue()
    {
        // Arrange
        var value1 = Unit.Value;
        var value2 = Unit.Value;

        // Act & Assert
        value1.Equals(value2).Should().BeTrue();
        (value1 == value2).Should().BeTrue();
    }

    [Fact]
    public void Unit_GetHashCode_ReturnsSameValue()
    {
        // Arrange
        var value1 = Unit.Value;
        var value2 = Unit.Value;

        // Act & Assert
        value1.GetHashCode().Should().Be(value2.GetHashCode());
    }

    [Fact]
    public void Unit_ToString_ReturnsExpectedValue()
    {
        // Arrange
        var value = Unit.Value;

        // Act
        var result = value.ToString();

        // Assert
        result.Should().Be("()");
    }

    [Fact]
    public void Unit_CompareTo_ReturnsZero()
    {
        // Arrange
        var value1 = Unit.Value;
        var value2 = Unit.Value;

        // Act
        var result = value1.CompareTo(value2);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void Unit_AsTaskResult_CanBeAwaited()
    {
        // Arrange
        var task = Task.FromResult(Unit.Value);

        // Act
        Func<Task> act = async () =>
        {
            var result = await task;
            result.Should().Be(Unit.Value);
        };

        // Assert
        act.Should().NotThrowAsync();
    }
}
