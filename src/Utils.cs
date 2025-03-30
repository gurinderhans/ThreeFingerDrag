using System;

namespace tfd
{
    public static class Utils
    {
        public static double CalculateHypotenuse(double a, double b)
        {
            return Math.Sqrt(Math.Pow(a, 2) + Math.Pow(b, 2));
        }

        public static int TranslateCoordToAbsolute(int coord, int width_or_height)
        {
            /// https://github.com/Lexikos/AutoHotkey_L/blob/master/source/keyboard_mouse.cpp#L2545
            return (((65536 * coord) / width_or_height) + (coord < 0 ? -1 : 1));
        }
    }
}
