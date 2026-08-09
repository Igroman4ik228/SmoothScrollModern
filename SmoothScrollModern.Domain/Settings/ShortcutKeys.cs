using Windows.System;

namespace SmoothScrollModern.Settings;

public static class ShortcutKeys
{
    public const VirtualKey Shift = VirtualKey.Shift;
    public const VirtualKey Control = VirtualKey.Control;
    public const VirtualKey Alt = VirtualKey.Menu;

    public const VirtualKey LeftShift = VirtualKey.LeftShift;
    public const VirtualKey RightShift = VirtualKey.RightShift;

    public const VirtualKey LeftControl = VirtualKey.LeftControl;
    public const VirtualKey RightControl = VirtualKey.RightControl;

    public const VirtualKey LeftAlt = VirtualKey.LeftMenu;
    public const VirtualKey RightAlt = VirtualKey.RightMenu;

    public const VirtualKey OemSemicolon = (VirtualKey)0xBA;
    public const VirtualKey OemPlus = (VirtualKey)0xBB;
    public const VirtualKey OemComma = (VirtualKey)0xBC;
    public const VirtualKey OemMinus = (VirtualKey)0xBD;
    public const VirtualKey OemPeriod = (VirtualKey)0xBE;
    public const VirtualKey OemSlash = (VirtualKey)0xBF;
    public const VirtualKey OemBackQuote = (VirtualKey)0xC0;
    public const VirtualKey OemOpenBracket = (VirtualKey)0xDB;
    public const VirtualKey OemBackslash = (VirtualKey)0xDC;
    public const VirtualKey OemCloseBracket = (VirtualKey)0xDD;
    public const VirtualKey OemQuote = (VirtualKey)0xDE;
    public const VirtualKey Oem8 = (VirtualKey)0xDF;
    public const VirtualKey Oem102 = (VirtualKey)0xE2;
    public const VirtualKey NumpadEquals = (VirtualKey)0x92;

    public const VirtualKey BrowserBack = (VirtualKey)0xA6;
    public const VirtualKey BrowserForward = (VirtualKey)0xA7;
    public const VirtualKey BrowserRefresh = (VirtualKey)0xA8;
    public const VirtualKey BrowserStop = (VirtualKey)0xA9;
    public const VirtualKey BrowserSearch = (VirtualKey)0xAA;
    public const VirtualKey BrowserFavorites = (VirtualKey)0xAB;
    public const VirtualKey BrowserHome = (VirtualKey)0xAC;

    public const VirtualKey VolumeMute = (VirtualKey)0xAD;
    public const VirtualKey VolumeDown = (VirtualKey)0xAE;
    public const VirtualKey VolumeUp = (VirtualKey)0xAF;
    public const VirtualKey MediaNextTrack = (VirtualKey)0xB0;
    public const VirtualKey MediaPreviousTrack = (VirtualKey)0xB1;
    public const VirtualKey MediaStop = (VirtualKey)0xB2;
    public const VirtualKey MediaPlayPause = (VirtualKey)0xB3;
    public const VirtualKey LaunchMail = (VirtualKey)0xB4;
    public const VirtualKey LaunchMediaSelect = (VirtualKey)0xB5;
    public const VirtualKey LaunchApplication1 = (VirtualKey)0xB6;
    public const VirtualKey LaunchApplication2 = (VirtualKey)0xB7;
    public const VirtualKey Power = (VirtualKey)0x5E;

    public const VirtualKey ImeOn = (VirtualKey)0x16;
    public const VirtualKey ImeOff = (VirtualKey)0x1A;
    public const VirtualKey ProcessKey = (VirtualKey)0xE5;
    public const VirtualKey Packet = (VirtualKey)0xE7;
    public const VirtualKey ImeAlphanumeric = (VirtualKey)0xF0;
    public const VirtualKey ImeKatakana = (VirtualKey)0xF1;
    public const VirtualKey ImeHiragana = (VirtualKey)0xF2;

    public static bool IsValid(VirtualKey virtualKey)
    {
        return (int)virtualKey is > 0 and < 255;
    }

    public static bool Matches(VirtualKey configuredVirtualKey, VirtualKey pressedVirtualKey)
    {
        if (configuredVirtualKey == pressedVirtualKey)
        {
            return true;
        }

        return configuredVirtualKey switch
        {
            VirtualKey.Shift => pressedVirtualKey is VirtualKey.LeftShift or VirtualKey.RightShift,
            VirtualKey.Control => pressedVirtualKey is VirtualKey.LeftControl or VirtualKey.RightControl,
            VirtualKey.Menu => pressedVirtualKey is VirtualKey.LeftMenu or VirtualKey.RightMenu,
            _ => false
        };
    }

    public static bool ContainsMatch(IEnumerable<VirtualKey> configuredVirtualKeys, VirtualKey pressedVirtualKey)
    {
        return configuredVirtualKeys.Any(configuredVirtualKey =>
            Matches(configuredVirtualKey, pressedVirtualKey));
    }

    public static bool ContainsConflict(IEnumerable<VirtualKey> configuredVirtualKeys, VirtualKey candidateVirtualKey)
    {
        return configuredVirtualKeys.Any(configuredVirtualKey =>
            Matches(configuredVirtualKey, candidateVirtualKey)
            || Matches(candidateVirtualKey, configuredVirtualKey));
    }

    public static IEnumerable<VirtualKey> ExpandGenericModifier(VirtualKey virtualKey)
    {
        return virtualKey switch
        {
            VirtualKey.Shift => [VirtualKey.LeftShift, VirtualKey.RightShift],
            VirtualKey.Control => [VirtualKey.LeftControl, VirtualKey.RightControl],
            VirtualKey.Menu => [VirtualKey.LeftMenu, VirtualKey.RightMenu],
            _ => [virtualKey]
        };
    }

    public static string Format(VirtualKey key)
    {
        if (key is >= VirtualKey.Number0 and <= VirtualKey.Number9)
        {
            return ((int)key - (int)VirtualKey.Number0).ToString();
        }

        return key switch
        {
            VirtualKey.Shift => "Shift",
            VirtualKey.LeftShift => "Left Shift",
            VirtualKey.RightShift => "Right Shift",

            VirtualKey.Control => "Ctrl",
            VirtualKey.LeftControl => "Left Ctrl",
            VirtualKey.RightControl => "Right Ctrl",

            VirtualKey.Menu => "Alt",
            VirtualKey.LeftMenu => "Left Alt",
            VirtualKey.RightMenu => "Right Alt",

            VirtualKey.LeftWindows => "Left Win",
            VirtualKey.RightWindows => "Right Win",

            VirtualKey.Escape => "Esc",
            VirtualKey.Back => "Backspace",
            VirtualKey.Tab => "Tab",
            VirtualKey.Enter => "Enter",
            VirtualKey.Space => "Space",
            VirtualKey.Pause => "Pause",
            VirtualKey.Clear => "Clear",
            VirtualKey.Cancel => "Cancel",
            VirtualKey.CapitalLock => "Caps Lock",
            VirtualKey.NumberKeyLock => "Num Lock",
            VirtualKey.Scroll => "Scroll Lock",
            VirtualKey.Snapshot => "Print Screen",
            VirtualKey.Print => "Print",

            VirtualKey.Insert => "Insert",
            VirtualKey.Delete => "Delete",
            VirtualKey.Home => "Home",
            VirtualKey.End => "End",
            VirtualKey.PageUp => "Page Up",
            VirtualKey.PageDown => "Page Down",
            VirtualKey.Left => "Left Arrow",
            VirtualKey.Up => "Up Arrow",
            VirtualKey.Right => "Right Arrow",
            VirtualKey.Down => "Down Arrow",

            VirtualKey.Application => "Context Menu",
            VirtualKey.Help => "Help",
            VirtualKey.Select => "Select",
            VirtualKey.Execute => "Execute",
            VirtualKey.Sleep => "Sleep",

            VirtualKey.NumberPad0 => "Num 0",
            VirtualKey.NumberPad1 => "Num 1",
            VirtualKey.NumberPad2 => "Num 2",
            VirtualKey.NumberPad3 => "Num 3",
            VirtualKey.NumberPad4 => "Num 4",
            VirtualKey.NumberPad5 => "Num 5",
            VirtualKey.NumberPad6 => "Num 6",
            VirtualKey.NumberPad7 => "Num 7",
            VirtualKey.NumberPad8 => "Num 8",
            VirtualKey.NumberPad9 => "Num 9",

            VirtualKey.Add => "Num +",
            VirtualKey.Subtract => "Num -",
            VirtualKey.Multiply => "Num *",
            VirtualKey.Divide => "Num /",
            VirtualKey.Decimal => "Num .",
            VirtualKey.Separator => "Num Separator",
            NumpadEquals => "Num =",

            OemSemicolon => ";",
            OemPlus => "=",
            OemComma => ",",
            OemMinus => "-",
            OemPeriod => ".",
            OemSlash => "/",
            OemBackQuote => "`",
            OemOpenBracket => "[",
            OemBackslash => "\\",
            OemCloseBracket => "]",
            OemQuote => "'",
            Oem8 => "OEM 8",
            Oem102 => "\\",

            BrowserBack => "Browser Back",
            BrowserForward => "Browser Forward",
            BrowserRefresh => "Browser Refresh",
            BrowserStop => "Browser Stop",
            BrowserSearch => "Browser Search",
            BrowserFavorites => "Browser Favorites",
            BrowserHome => "Browser Home",

            VolumeMute => "Volume Mute",
            VolumeDown => "Volume Down",
            VolumeUp => "Volume Up",
            MediaNextTrack => "Media Next",
            MediaPreviousTrack => "Media Previous",
            MediaStop => "Media Stop",
            MediaPlayPause => "Media Play/Pause",
            LaunchMail => "Mail",
            LaunchMediaSelect => "Media Select",
            LaunchApplication1 => "App 1",
            LaunchApplication2 => "App 2",
            Power => "Power",

            VirtualKey.Accept => "IME Accept",
            VirtualKey.Convert => "IME Convert",
            VirtualKey.Final => "IME Final",
            VirtualKey.Hangul => "Hangul",
            VirtualKey.Hanja => "Hanja",
            VirtualKey.Junja => "Junja",
            VirtualKey.ModeChange => "IME Mode Change",
            VirtualKey.NonConvert => "IME NonConvert",
            ImeOn => "IME On",
            ImeOff => "IME Off",
            ProcessKey => "IME Process",
            Packet => "Packet",
            ImeAlphanumeric => "IME Alphanumeric",
            ImeKatakana => "Katakana",
            ImeHiragana => "Hiragana",

            _ => Enum.IsDefined(key)
                ? key.ToString()
                : $"Неизвестная клавиша: {(int)key}"
        };
    }
}
