using System;
using System.Runtime.InteropServices;
using SnippetLauncher.Core.Interfaces;

namespace SnippetLauncher.Infrastructure.Services;

public class HotkeyService : IHotkeyService
{
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int HOTKEY_ID = 9000;

    private Action? _onHotkeyTriggered;
    private bool _registered;
    private IntPtr _windowHandle;

    public void RegisterGlobalHotkey(Action onHotkeyTriggered)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        _onHotkeyTriggered = onHotkeyTriggered;
    }

    public bool TryRegister(IntPtr windowHandle, Action onHotkeyTriggered, string modifiersStr, string keyStr)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return false;

        UnregisterGlobalHotkey();

        _onHotkeyTriggered = onHotkeyTriggered;
        _windowHandle = windowHandle;

        uint modifiers = ParseModifiers(modifiersStr);
        uint key = ParseKey(keyStr);

        _registered = RegisterHotKey(windowHandle, HOTKEY_ID, modifiers, key);
        return _registered;
    }

    public void Register(IntPtr windowHandle, Action onHotkeyTriggered)
    {
        TryRegister(windowHandle, onHotkeyTriggered, "Control, Alt", "Space");
    }

    public void UnregisterGlobalHotkey()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || !_registered || _windowHandle == IntPtr.Zero) return;

        UnregisterHotKey(_windowHandle, HOTKEY_ID);
        _registered = false;
    }

    public IntPtr ProcessMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_HOTKEY = 0x0312;
        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            _onHotkeyTriggered?.Invoke();
            handled = true;
        }
        return IntPtr.Zero;
    }

    private uint ParseModifiers(string modifiersStr)
    {
        uint mods = 0;
        if (string.IsNullOrEmpty(modifiersStr)) return mods;

        var parts = modifiersStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var p in parts)
        {
            if (p.Equals("Control", StringComparison.OrdinalIgnoreCase)) mods |= 0x0002;
            else if (p.Equals("Alt", StringComparison.OrdinalIgnoreCase)) mods |= 0x0001;
            else if (p.Equals("Shift", StringComparison.OrdinalIgnoreCase)) mods |= 0x0004;
            else if (p.Equals("Win", StringComparison.OrdinalIgnoreCase)) mods |= 0x0008;
        }
        return mods;
    }

    private uint ParseKey(string keyStr)
    {
        if (string.IsNullOrEmpty(keyStr)) return 0;

        if (keyStr.Equals("Space", StringComparison.OrdinalIgnoreCase)) return 0x20;

        if (keyStr.Length == 1)
        {
            char c = char.ToUpper(keyStr[0]);
            if (c >= 'A' && c <= 'Z')
            {
                return (uint)c;
            }
        }

        return 0; // Default or unhandled
    }
}
