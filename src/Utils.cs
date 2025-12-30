namespace tfd
{
    using System;

    public static class Utils
    {
        public static double CalculateHypotenuse(double a, double b)
        {
            return Math.Sqrt(Math.Pow(a, 2) + Math.Pow(b, 2));
        }

        public static double ClampValue(double value, double min, double max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}
