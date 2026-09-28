using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using SnippetLauncher.Core.Interfaces;
using SnippetLauncher.Core.Models;

namespace SnippetLauncher.Infrastructure.Services;

public interface ISystemClipboard
{
    string GetText();
    void SetText(string text);
    void SimulatePaste();
    bool ContainsText();
}

public class SystemClipboard : ISystemClipboard
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion u;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public MOUSEINPUT mi;
        [FieldOffset(0)]
        public KEYBDINPUT ki;
        [FieldOffset(0)]
        public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
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

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    private const int INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const ushort VK_CONTROL = 0x11;
    private const ushort VK_V = 0x56;

    public bool ContainsText()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return true; // Mock true for tests on Linux
        }

        bool hasText = false;
        var thread = new Thread(() =>
        {
            try
            {
                var assembly = System.Reflection.Assembly.Load("PresentationCore, Version=10.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
                var clipboardType = assembly.GetType("System.Windows.Clipboard");
                var method = clipboardType?.GetMethod("ContainsText", Type.EmptyTypes);
                var result = method?.Invoke(null, null);
                if (result is bool b) hasText = b;
            }
            catch { }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        return hasText;
    }

    public string GetText()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "Mock Clipboard text on non-Windows";
        }

        string text = string.Empty;
        var thread = new Thread(() =>
        {
            try
            {
                var assembly = System.Reflection.Assembly.Load("PresentationCore, Version=10.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
                var clipboardType = assembly.GetType("System.Windows.Clipboard");
                var method = clipboardType?.GetMethod("GetText", Type.EmptyTypes);
                text = method?.Invoke(null, null) as string ?? string.Empty;
            }
            catch { }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        return text;
    }

    public void SetText(string text)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

        var thread = new Thread(() =>
        {
            try
            {
                var assembly = System.Reflection.Assembly.Load("PresentationCore, Version=10.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
                var clipboardType = assembly.GetType("System.Windows.Clipboard");

                if (string.IsNullOrEmpty(text))
                {
                    var method = clipboardType?.GetMethod("Clear", Type.EmptyTypes);
                    method?.Invoke(null, null);
                }
                else
                {
                    var method = clipboardType?.GetMethod("SetText", new[] { typeof(string) });
                    method?.Invoke(null, new object[] { text });
                }
            }
            catch { }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
    }

    public void SimulatePaste()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

        INPUT[] inputs = new INPUT[4];

        // Key down Control
        inputs[0].type = INPUT_KEYBOARD;
        inputs[0].u.ki.wVk = VK_CONTROL;

        // Key down V
        inputs[1].type = INPUT_KEYBOARD;
        inputs[1].u.ki.wVk = VK_V;

        // Key up V
        inputs[2].type = INPUT_KEYBOARD;
        inputs[2].u.ki.wVk = VK_V;
        inputs[2].u.ki.dwFlags = KEYEVENTF_KEYUP;

        // Key up Control
        inputs[3].type = INPUT_KEYBOARD;
        inputs[3].u.ki.wVk = VK_CONTROL;
        inputs[3].u.ki.dwFlags = KEYEVENTF_KEYUP;

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
        // 1. Save current clipboard only if it's text (ignore files, images to save memory/avoid crashes)
        string currentClipboard = string.Empty;
        bool hasTextBackup = false;

        if (_systemClipboard.ContainsText())
        {
            currentClipboard = _systemClipboard.GetText();
            hasTextBackup = true;
        }

        // 2. Parse variables
        var parsedContent = _parser.Parse(snippet.Code, currentClipboard);

        // 3. Set clipboard to parsed snippet
        _systemClipboard.SetText(parsedContent);

        // 4. Simulate Paste (Ctrl+V)
        _systemClipboard.SimulatePaste();

        // 5. Short delay to ensure paste went through
        await Task.Delay(100);

        // 6. Restore original clipboard if we had one
        if (hasTextBackup)
        {
            _systemClipboard.SetText(currentClipboard);
        }
        else
        {
            _systemClipboard.SetText(string.Empty); // clear if it was an image/file
        }
    }
}
