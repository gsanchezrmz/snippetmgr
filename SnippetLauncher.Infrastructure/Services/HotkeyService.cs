using System;
using System.Runtime.InteropServices;
using SnippetLauncher.Core.Interfaces;

namespace SnippetLauncher.Infrastructure.Services;

public class HotkeyService : IHotkeyService
{
    // Simplified Hotkey implementation for Windows
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int HOTKEY_ID = 9000;
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint VK_SPACE = 0x20;

    private Action? _onHotkeyTriggered;
    private bool _registered;
    private IntPtr _windowHandle;

    public void RegisterGlobalHotkey(Action onHotkeyTriggered)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        _onHotkeyTriggered = onHotkeyTriggered;
        // In real app, we need window handle here, but interface only allows Action.
        // We added Register(IntPtr, Action) for actual use.
    }

    public void Register(IntPtr windowHandle, Action onHotkeyTriggered)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

        _onHotkeyTriggered = onHotkeyTriggered;
        _windowHandle = windowHandle;

        // HwndSource is WPF specific, so we omit it here in infrastructure which targets net8.0
        // The WPF app will wire up the hook.

        _registered = RegisterHotKey(windowHandle, HOTKEY_ID, MOD_ALT | MOD_CONTROL, VK_SPACE);
    }

    public void UnregisterGlobalHotkey()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || !_registered || _windowHandle == IntPtr.Zero) return;

        UnregisterHotKey(_windowHandle, HOTKEY_ID);
        _registered = false;
    }

    // Called from MainWindow hook
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
}
