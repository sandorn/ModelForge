using ModelForge.Sidecar.Keyboard;
using Xunit;

namespace ModelForge.Sidecar.Tests;

public class ChordParserTests
{
    [Theory]
    [InlineData(ChordParser.VKey.A, true, false, false, "Ctrl+A")]
    [InlineData(ChordParser.VKey.A, false, true, false, "Alt+A")]
    [InlineData(ChordParser.VKey.A, false, false, true, "Shift+A")]
    [InlineData(ChordParser.VKey.A, true, true, false, "Ctrl+Alt+A")]
    [InlineData(ChordParser.VKey.A, true, false, true, "Ctrl+Shift+A")]
    [InlineData(ChordParser.VKey.A, false, true, true, "Alt+Shift+A")]
    [InlineData(ChordParser.VKey.A, true, true, true, "Ctrl+Alt+Shift+A")]
    public void BuildChord_Modifiers_YieldCorrectOrder(uint vk, bool ctrl, bool alt, bool shift, string expected)
    {
        var result = ChordParser.BuildChord(vk, ctrl, alt, shift);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(ChordParser.VKey.D0, false, false, false, "0")]
    [InlineData(ChordParser.VKey.D0 + 5, false, false, false, "5")]  // D5
    [InlineData(ChordParser.VKey.D9, false, false, false, "9")]
    public void BuildChord_Digits_YieldCorrectChar(uint vk, bool ctrl, bool alt, bool shift, string expected)
    {
        var result = ChordParser.BuildChord(vk, ctrl, alt, shift);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(ChordParser.VKey.F1, "F1")]
    [InlineData(ChordParser.VKey.F1 + 4, "F5")]    // F5
    [InlineData(ChordParser.VKey.F1 + 11, "F12")]  // F12
    [InlineData(ChordParser.VKey.F1 + 23, "F24")]  // F24]
    public void BuildChord_FunctionKeys_YieldCorrectName(uint vk, string expected)
    {
        var result = ChordParser.BuildChord(vk, false, false, false);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(ChordParser.VKey.Enter, "Enter")]
    [InlineData(ChordParser.VKey.Space, "Space")]
    [InlineData(ChordParser.VKey.Tab, "Tab")]
    [InlineData(ChordParser.VKey.Escape, "Esc")]
    [InlineData(ChordParser.VKey.Delete, "Delete")]
    [InlineData(ChordParser.VKey.Backspace, "Backspace")]
    [InlineData(ChordParser.VKey.Left, "Left")]
    [InlineData(ChordParser.VKey.Right, "Right")]
    [InlineData(ChordParser.VKey.Up, "Up")]
    [InlineData(ChordParser.VKey.Down, "Down")]
    public void BuildChord_SpecialKeys_YieldCorrectName(uint vk, string expected)
    {
        var result = ChordParser.BuildChord(vk, false, false, false);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(ChordParser.VKey.OemMinus, "-")]
    [InlineData(ChordParser.VKey.OemPlus, "=")]
    [InlineData(ChordParser.VKey.OemPeriod, ".")]
    [InlineData(ChordParser.VKey.OemComma, ",")]
    public void BuildChord_OemKeys_YieldCorrectChar(uint vk, string expected)
    {
        var result = ChordParser.BuildChord(vk, false, false, false);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildChord_NumPad_YieldsNumPadPrefix()
    {
        var result = ChordParser.BuildChord(ChordParser.VKey.NumPad0, false, false, false);
        Assert.Equal("NumPad0", result);

        var result9 = ChordParser.BuildChord(ChordParser.VKey.NumPad9, false, false, false);
        Assert.Equal("NumPad9", result9);
    }

    [Theory]
    [InlineData(0xFF, false, false, false)] // 未知键
    public void BuildChord_UnknownKey_ReturnsEmpty(uint vk, bool c, bool a, bool s)
    {
        var result = ChordParser.BuildChord(vk, c, a, s);
        Assert.Equal(string.Empty, result);
    }
}
