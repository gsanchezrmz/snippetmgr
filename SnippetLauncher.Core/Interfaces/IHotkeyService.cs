using System;

namespace SnippetLauncher.Core.Interfaces;

public interface IHotkeyService
{
    void RegisterGlobalHotkey(Action onHotkeyTriggered);
    void UnregisterGlobalHotkey();
}
