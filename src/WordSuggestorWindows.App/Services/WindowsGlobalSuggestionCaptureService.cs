using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

public sealed class WindowsGlobalSuggestionCaptureService : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WmKeyDown = 0x0100;
    private const int WmSysKeyDown = 0x0104;
    private const int VkBack = 0x08;
    private const int VkTab = 0x09;
    private const int VkReturn = 0x0D;
    private const int VkEscape = 0x1B;
    private const int VkControl = 0x11;
    private const int VkMenu = 0x12;
    private const int VkLwin = 0x5B;
    private const int VkRwin = 0x5C;
    private const int VkShift = 0x10;
    private const int VkCapital = 0x14;
    private const uint MapVkToVsc = 0;
    private readonly LowLevelKeyboardProc _callback;
    private IntPtr _hookHandle;
    private IntPtr _ignoredWindowHandle;
    private IntPtr _currentExternalWindowHandle;
    private string _currentToken = string.Empty;
    private DateTimeOffset _suppressSyntheticInputUntil = DateTimeOffset.MinValue;

    public WindowsGlobalSuggestionCaptureService()
    {
        _callback = KeyboardHookCallback;
    }

    public event EventHandler<SelectionImportDiagnostic>? DiagnosticEmitted;

    public event EventHandler<ExternalSuggestionTokenChangedEventArgs>? TokenChanged;

    public bool IsRunning => _hookHandle != IntPtr.Zero;

    public string CurrentToken => _currentToken;

    public void Start(IntPtr ignoredWindowHandle)
    {
        _ignoredWindowHandle = ignoredWindowHandle;
        if (_hookHandle != IntPtr.Zero)
        {
            return;
        }

        using var process = Process.GetCurrentProcess();
        using var module = process.MainModule;
        var moduleHandle = module is null ? IntPtr.Zero : GetModuleHandle(module.ModuleName);
        _hookHandle = SetWindowsHookEx(WhKeyboardLl, _callback, moduleHandle, 0);
        if (_hookHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException(
                $"Global suggestion keyboard hook could not start. GetLastWin32Error={Marshal.GetLastWin32Error()}");
        }

        EmitDiagnostic("GlobalSuggestionCapture", "HookStarted", "Low-level keyboard hook registered for external suggestion capture.");
    }

    public void Stop()
    {
        if (_hookHandle == IntPtr.Zero)
        {
            return;
        }

        _ = UnhookWindowsHookEx(_hookHandle);
        _hookHandle = IntPtr.Zero;
        EmitDiagnostic("GlobalSuggestionCapture", "HookStopped", "Low-level keyboard hook was stopped.");
        ResetCurrentToken(emitBoundary: false);
    }

    public void SuppressSyntheticInput(TimeSpan duration)
    {
        _suppressSyntheticInputUntil = DateTimeOffset.UtcNow.Add(duration);
    }

    public void ClearTokenAfterCommittedSuggestion()
    {
        _currentToken = string.Empty;
    }

    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0)
        {
            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        if (DateTimeOffset.UtcNow < _suppressSyntheticInputUntil)
        {
            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        var message = wParam.ToInt32();
        if (message is not WmKeyDown and not WmSysKeyDown)
        {
            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        var keyboardInfo = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
        var foregroundWindow = GetForegroundWindow();
        if (foregroundWindow == IntPtr.Zero || foregroundWindow == _ignoredWindowHandle)
        {
            ResetCurrentToken(emitBoundary: false);
            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        if (_currentExternalWindowHandle != foregroundWindow)
        {
            ResetCurrentToken(emitBoundary: false);
            _currentExternalWindowHandle = foregroundWindow;
        }

        if (HasUnsupportedModifiers())
        {
            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        switch ((int)keyboardInfo.vkCode)
        {
            case VkBack:
                HandleBackspace(foregroundWindow);
                break;
            case VkTab:
            case VkReturn:
            case VkEscape:
                ResetCurrentToken(emitBoundary: true);
                break;
            default:
                HandleTextInput(foregroundWindow, keyboardInfo);
                break;
        }

        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    private void HandleBackspace(IntPtr foregroundWindow)
    {
        if (_currentToken.Length == 0)
        {
            return;
        }

        _currentToken = _currentToken[..^1];
        EmitTokenChanged(foregroundWindow, _currentToken, isBoundary: false);
    }

    private void HandleTextInput(IntPtr foregroundWindow, KBDLLHOOKSTRUCT keyboardInfo)
    {
        if (!TryTranslateToText(foregroundWindow, keyboardInfo, out var text) || string.IsNullOrEmpty(text))
        {
            return;
        }

        if (IsTokenText(text))
        {
            _currentToken += text;
            EmitTokenChanged(foregroundWindow, _currentToken, isBoundary: false);
            return;
        }

        if (ContainsBoundary(text))
        {
            ResetCurrentToken(emitBoundary: true);
        }
    }

    private void ResetCurrentToken(bool emitBoundary)
    {
        if (!emitBoundary && _currentToken.Length == 0)
        {
            _currentExternalWindowHandle = IntPtr.Zero;
            return;
        }

        var windowHandle = _currentExternalWindowHandle;
        _currentToken = string.Empty;
        if (emitBoundary && windowHandle != IntPtr.Zero)
        {
            EmitTokenChanged(windowHandle, string.Empty, isBoundary: true);
        }

        if (!emitBoundary)
        {
            _currentExternalWindowHandle = IntPtr.Zero;
        }
    }

    private void EmitTokenChanged(IntPtr windowHandle, string token, bool isBoundary)
    {
        EmitDiagnostic(
            "GlobalSuggestionCapture",
            isBoundary ? "Boundary" : "TokenChanged",
            $"{DescribeWindowHandle(windowHandle)} token=\"{token}\" length={token.Length}");
        TokenChanged?.Invoke(
            this,
            new ExternalSuggestionTokenChangedEventArgs(windowHandle, token, isBoundary, DateTimeOffset.Now));
    }

    private static bool HasUnsupportedModifiers()
    {
        var ctrl = (GetKeyState(VkControl) & 0x8000) != 0;
        var alt = (GetKeyState(VkMenu) & 0x8000) != 0;
        var lwin = (GetKeyState(VkLwin) & 0x8000) != 0;
        var rwin = (GetKeyState(VkRwin) & 0x8000) != 0;
        return ctrl || alt || lwin || rwin;
    }

    private static bool TryTranslateToText(IntPtr foregroundWindow, KBDLLHOOKSTRUCT keyboardInfo, out string text)
    {
        text = string.Empty;
        var keyboardState = new byte[256];
        if (!GetKeyboardState(keyboardState))
        {
            return false;
        }

        var scanCode = MapVirtualKey(keyboardInfo.vkCode, MapVkToVsc);
        var foregroundThreadId = GetWindowThreadProcessId(foregroundWindow, out _);
        var layout = GetKeyboardLayout(foregroundThreadId);
        Span<char> buffer = stackalloc char[8];
        var result = ToUnicodeEx(
            keyboardInfo.vkCode,
            scanCode,
            keyboardState,
            buffer,
            buffer.Length,
            0,
            layout);

        if (result <= 0)
        {
            return false;
        }

        text = new string(buffer[..result]);
        return true;
    }

    private void EmitDiagnostic(string stage, string outcome, string detail)
    {
        DiagnosticEmitted?.Invoke(this, new SelectionImportDiagnostic(DateTimeOffset.Now, stage, outcome, detail));
    }

    private static string DescribeWindowHandle(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
        {
            return "hwnd=0x0";
        }

        var threadId = GetWindowThreadProcessId(windowHandle, out var processId);
        return $"hwnd=0x{windowHandle.ToInt64():X}, tid={threadId}, pid={processId}";
    }

    private static bool IsTokenText(string text) =>
        text.All(static c => char.IsLetterOrDigit(c) || c is '\'' or '-');

    private static bool ContainsBoundary(string text) =>
        text.Any(static c => char.IsWhiteSpace(c) || char.IsPunctuation(c) || char.IsSymbol(c));

    public void Dispose()
    {
        Stop();
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int nVirtKey);

    [DllImport("user32.dll")]
    private static extern bool GetKeyboardState(byte[] lpKeyState);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint uCode, uint uMapType);

    [DllImport("user32.dll")]
    private static extern IntPtr GetKeyboardLayout(uint idThread);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int ToUnicodeEx(
        uint wVirtKey,
        uint wScanCode,
        byte[] lpKeyState,
        Span<char> pwszBuff,
        int cchBuff,
        uint wFlags,
        IntPtr dwhkl);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);
}
