using System.Drawing;
using static Quackers.TestLogger.ConsoleColors;

namespace Quackers.TestLogger
{
    public interface ITheme
    {
        Color Fail { get; }
        Color Pass { get; }
        Color Debug { get; }
        Color StackTrace { get; }
        Color Error { get; }
        Color Disabled { get; }
        Color DisabledReason { get; }
    }

    public class DefaultTheme: ITheme
    {
        public Color Fail { get; } = BrightRed;
        public Color Pass { get; } = BrightGreen;
        public Color Debug { get; } = BrightBlue;
        public Color StackTrace { get; } = BrightCyan;
        public Color Error { get; } = BrightMagenta;
        public Color Disabled { get; } = Grey;
        public Color DisabledReason { get; } = DarkGrey;
    }

    public class DarkerTheme
        : ITheme
    {
        public Color Fail { get; } = Red;
        public Color Pass { get; } = Green;
        public Color Debug { get; } = Blue;
        public Color StackTrace { get; } = Cyan;
        public Color Error { get; } = Magenta;
        public Color Disabled { get; } = Grey;
        public Color DisabledReason { get; } = LightGrey;
    }

    public static class ConsoleColors
    {
        private const int LighterPrimaryValue = 225;
        private const int LighterSecondaryValue = 110;
        private const int DarkGreyValue = 80;
        public static readonly Color BrightRed = Color.FromArgb(255, LighterPrimaryValue, LighterSecondaryValue, LighterSecondaryValue);
        public static readonly Color BrightGreen = Color.FromArgb(255, LighterSecondaryValue, LighterPrimaryValue, 0);
        public static readonly Color BrightBlue = Color.FromArgb(255, LighterSecondaryValue, LighterSecondaryValue, LighterPrimaryValue);
        public static readonly Color BrightCyan = Color.FromArgb(255, LighterSecondaryValue, LighterPrimaryValue, LighterPrimaryValue);
        public static readonly Color BrightMagenta = Color.FromArgb(255, LighterPrimaryValue, LighterSecondaryValue, LighterPrimaryValue);
        public static readonly Color DarkGrey = Color.FromArgb(255, DarkGreyValue, DarkGreyValue, DarkGreyValue);
        
        private const int DarkerPrimaryValue = 140;
        private const int LightGreyValue = 170;
        public static readonly Color Red = Color.FromArgb(255, DarkerPrimaryValue, 0, 0);
        public static readonly Color Green = Color.FromArgb(255, 0, DarkerPrimaryValue, 0);
        public static readonly Color Blue = Color.FromArgb(255, 0, 0, DarkerPrimaryValue);
        public static readonly Color Cyan = Color.FromArgb(255, 0, DarkerPrimaryValue, DarkerPrimaryValue);
        public static readonly Color Magenta = Color.FromArgb(255, DarkerPrimaryValue, 0, DarkerPrimaryValue);
        public static readonly Color Grey = Color.FromArgb(255, LighterSecondaryValue, LighterSecondaryValue, LighterSecondaryValue);
        public static readonly Color LightGrey = Color.FromArgb(255, LightGreyValue, LightGreyValue, LightGreyValue);
    }
}