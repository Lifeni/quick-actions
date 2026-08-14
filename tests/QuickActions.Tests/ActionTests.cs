using System.Text.Json;
using QuickActions.Actions;
using QuickActions.Core;

namespace QuickActions.Tests;

public class DisplayModeActionTests
{
    [Theory]
    [InlineData("internal", 0x00000001)]
    [InlineData("clone", 0x00000002)]
    [InlineData("extend", 0x00000004)]
    [InlineData("external", 0x00000008)]
    public void TopologyFor_KnownModes_MapsToFlag(string mode, uint expectedFlag)
    {
        Assert.Equal(expectedFlag, DisplayModeAction.TopologyFor(mode));
    }

    [Theory]
    [InlineData("INTERNAL")]
    [InlineData("Extend")]
    [InlineData("EXT")]
    public void ExtractMode_StringArg_IsTrimmedAndLowercased(string input)
    {
        Assert.Equal(input.Trim().ToLowerInvariant(), DisplayModeAction.ExtractMode(input));
    }

    [Fact]
    public void ExtractMode_JsonObjectArg_ReadsModeProperty()
    {
        var json = JsonDocument.Parse("""{ "mode": "extend" }""").RootElement;

        Assert.Equal("extend", DisplayModeAction.ExtractMode(json));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void ExtractMode_MissingOrBlank_Throws(string? input)
    {
        Assert.Throws<ArgumentException>(() => DisplayModeAction.ExtractMode(input));
    }

    [Fact]
    public void ExtractMode_JsonWithoutMode_Throws()
    {
        var json = JsonDocument.Parse("""{ "other": 1 }""").RootElement;

        Assert.Throws<ArgumentException>(() => DisplayModeAction.ExtractMode(json));
    }

    [Fact]
    public void TopologyFor_UnknownMode_Throws()
    {
        Assert.Throws<ArgumentException>(() => DisplayModeAction.TopologyFor("triple"));
    }

    [Fact]
    public void Action_ExposesExpectedName()
    {
        Assert.Equal("display_mode", new DisplayModeAction().Name);
    }
}

public class ActionRegistryTests
{
    [Fact]
    public void Find_RegisteredAction_ReturnsInstance()
    {
        var registry = new ActionRegistry();
        var action = new DisplayModeAction();
        registry.Register(action);

        Assert.Same(action, registry.Find("display_mode"));
    }

    [Fact]
    public void Find_IsCaseInsensitive()
    {
        var registry = new ActionRegistry();
        registry.Register(new DisplayModeAction());

        Assert.NotNull(registry.Find("DISPLAY_MODE"));
        Assert.NotNull(registry.Find("Display_Mode"));
    }

    [Fact]
    public void Find_UnknownAction_ReturnsNull()
    {
        var registry = new ActionRegistry();

        Assert.Null(registry.Find("no_such_action"));
    }

    [Fact]
    public void Register_SameName_Overwrites()
    {
        var registry = new ActionRegistry();
        var first = new DisplayModeAction();
        var second = new DisplayModeAction();
        registry.Register(first);
        registry.Register(second);

        Assert.Same(second, registry.Find("display_mode"));
    }
}
