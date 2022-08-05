using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Pastel;

namespace Quackers.TestLogger
{
    public static class StringExtensions
    {
        public static bool DisableColor { get; set; } = false;
        private static readonly Color BrightRedColor = Color.FromArgb(255, 255, 128, 128);
        private static readonly Color BrightGreenColor = Color.FromArgb(255, 128, 255, 0);
        private static readonly Color BrightBlueColor = Color.FromArgb(255, 128, 128, 255);
        private static readonly Color BrightCyanColor = Color.FromArgb(255, 128, 255, 255);
        private static readonly Color BrightYellowColor = Color.FromArgb(255, 255, 255, 128);
        private static readonly Color BrightMagentaColor = Color.FromArgb(255, 255, 128, 255);
        private static readonly Color BrightPinkColor = Color.FromArgb(255, 255, 128, 160);
        private static readonly Color LightGreyColor = Color.FromArgb(255, 128, 128, 128);
        private static readonly Color DarkGreyColor = Color.FromArgb(255, 80, 80, 80);

        public static string BrightRed(this string str)
        {
            return DisableColor
                ? str
                : str.Pastel(BrightRedColor);
        }

        public static string BrightGreen(this string str)
        {
            return DisableColor
                ? str
                : str.Pastel(BrightGreenColor);
        }

        public static string BrightCyan(this string str)
        {
            return DisableColor
                ? str
                : str.Pastel(BrightCyanColor);
        }

        public static string BrightYellow(this string str)
        {
            return DisableColor
                ? str
                : str.Pastel(BrightYellowColor);
        }

        public static string BrightMagenta(this string str)
        {
            return DisableColor
                ? str
                : str.Pastel(BrightMagentaColor);
        }

        public static string BrightPink(this string str)
        {
            return DisableColor
                ? str
                : str.Pastel(BrightPinkColor);
        }

        public static string BrightBlue(this string str)
        {
            return DisableColor
                ? str
                : str.Pastel(BrightBlueColor);
        }


        public static string Grey(this string str)
        {
            return DisableColor
                ? str
                : str.Pastel(LightGreyColor);
        }

        public static string DarkGrey(this string str)
        {
            return DisableColor
                ? str
                : str.Pastel(DarkGreyColor);
        }

        public static string DefaultTo(this string str, string fallback)
        {
            return string.IsNullOrWhiteSpace(str)
                ? fallback
                : str;
        }
    }
}