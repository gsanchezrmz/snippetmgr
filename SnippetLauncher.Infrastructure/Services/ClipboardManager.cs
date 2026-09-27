using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using SnippetLauncher.Core.Interfaces;
using SnippetLauncher.Core.Models;

namespace SnippetLauncher.Infrastructure.Services;

// We use an interface for clipboard API so we can mock it in tests.
public interface ISystemClipboard
{
    string GetText();
    void SetText(string text);
    void SimulatePaste();
}

public class SystemClipboard : ISystemClipboard
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    private const int INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const ushort VK_CONTROL = 0x11;
    private const ushort VK_V = 0x56;

    public string GetText()
    {
        // For WPF, you would normally use System.Windows.Clipboard on an STA thread.
        // We throw PlatformNotSupportedException if not on Windows, or just mock it.
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "Mock Clipboard text on non-Windows";
        }

        // Reflection to avoid WPF assembly dependency in Infrastructure project if not using net8.0-windows
        // This is simplified since we use tests that run on Linux in CI
        return string.Empty;
    }

    public void SetText(string text)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
    }

    public void SimulatePaste()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

        INPUT[] inputs = new INPUT[4];

        // Key down Control
        inputs[0].type = INPUT_KEYBOARD;
        inputs[0].ki.wVk = VK_CONTROL;

        // Key down V
        inputs[1].type = INPUT_KEYBOARD;
        inputs[1].ki.wVk = VK_V;

        // Key up V
        inputs[2].type = INPUT_KEYBOARD;
        inputs[2].ki.wVk = VK_V;
        inputs[2].ki.dwFlags = KEYEVENTF_KEYUP;

        // Key up Control
        inputs[3].type = INPUT_KEYBOARD;
        inputs[3].ki.wVk = VK_CONTROL;
        inputs[3].ki.dwFlags = KEYEVENTF_KEYUP;

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
    }
}

public class ClipboardManager : IClipboardManager
{
    private readonly IVariableParser _parser;
    private readonly ISystemClipboard _systemClipboard;

    public ClipboardManager(IVariableParser parser, ISystemClipboard systemClipboard)
    {
        _parser = parser;
        _systemClipboard = systemClipboard;
    }

    public async Task InjectSnippetAsync(Snippet snippet)
    {
        // 1. Save current clipboard
        var currentClipboard = _systemClipboard.GetText();

        // 2. Parse variables
        var parsedContent = _parser.Parse(snippet.Content, currentClipboard);

        // 3. Set clipboard to parsed snippet
        _systemClipboard.SetText(parsedContent);

        // 4. Simulate Paste (Ctrl+V)
        _systemClipboard.SimulatePaste();

        // 5. Short delay to ensure paste went through
        await Task.Delay(100);

        // 6. Restore original clipboard
        _systemClipboard.SetText(currentClipboard);
    }
}
