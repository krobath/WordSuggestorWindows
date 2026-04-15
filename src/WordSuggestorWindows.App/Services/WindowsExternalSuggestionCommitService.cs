using System.Runtime.InteropServices;
using System.Text;

namespace WordSuggestorWindows.App.Services;

public sealed class WindowsExternalSuggestionCommitService
{
    private const uint InputKeyboard = 1;
    private const uint KeyEventKeyUp = 0x0002;
    private const uint KeyEventUnicode = 0x0004;
    private const ushort VirtualKeyBack = 0x08;

    public bool TryCommitSuggestion(
        IntPtr excludedWindowHandle,
        string replacement,
        int replaceCharacterCount,
        bool appendTrailingSpace,
        out string diagnostic)
    {
        diagnostic = string.Empty;

        var targetWindow = GetForegroundWindow();
        if (targetWindow == IntPtr.Zero)
        {
            diagnostic = "Ingen foreground-app var tilgængelig for commit.";
            return false;
        }

        if (targetWindow == excludedWindowHandle)
        {
            diagnostic = "Foreground-vinduet var WordSuggestor i stedet for den eksterne app.";
            return false;
        }

        var inputs = BuildInputs(replacement, replaceCharacterCount, appendTrailingSpace);
        if (inputs.Count == 0)
        {
            diagnostic = "Ingen tastaturinput blev genereret til commit.";
            return false;
        }

        var sent = SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<INPUT>());
        if (sent != inputs.Count)
        {
            diagnostic = $"SendInput sendte kun {sent}/{inputs.Count} events. GetLastWin32Error={Marshal.GetLastWin32Error()}.";
            return false;
        }

        diagnostic = $"Commit sendte {sent} tastatur-events til foreground-appen.";
        return true;
    }

    private static List<INPUT> BuildInputs(string replacement, int replaceCharacterCount, bool appendTrailingSpace)
    {
        var inputs = new List<INPUT>(Math.Max(8, (replaceCharacterCount * 2) + (replacement.Length * 2) + 2));

        for (var i = 0; i < replaceCharacterCount; i++)
        {
            inputs.Add(CreateVirtualKeyInput(VirtualKeyBack, keyUp: false));
            inputs.Add(CreateVirtualKeyInput(VirtualKeyBack, keyUp: true));
        }

        foreach (var rune in replacement.EnumerateRunes())
        {
            var chars = rune.ToString();
            foreach (var c in chars)
            {
                inputs.Add(CreateUnicodeInput(c, keyUp: false));
                inputs.Add(CreateUnicodeInput(c, keyUp: true));
            }
        }

        if (appendTrailingSpace)
        {
            inputs.Add(CreateUnicodeInput(' ', keyUp: false));
            inputs.Add(CreateUnicodeInput(' ', keyUp: true));
        }

        return inputs;
    }

    private static INPUT CreateVirtualKeyInput(ushort virtualKey, bool keyUp) =>
        new()
        {
            type = InputKeyboard,
            U = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = virtualKey,
                    wScan = 0,
                    dwFlags = keyUp ? KeyEventKeyUp : 0,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

    private static INPUT CreateUnicodeInput(char value, bool keyUp) =>
        new()
        {
            type = InputKeyboard,
            U = new INPUTUNION
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0,
                    wScan = value,
                    dwFlags = KeyEventUnicode | (keyUp ? KeyEventKeyUp : 0),
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            }
        };

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public INPUTUNION U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct INPUTUNION
    {
        [FieldOffset(0)]
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
}
