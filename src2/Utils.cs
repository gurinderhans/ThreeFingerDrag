namespace tpb
{
    using System.Drawing;
    using System.Drawing.Drawing2D;

    public static class Utils
    {
        public static bool IsPointInPolygon(int pointX, int pointY, Point[] polygon)
        {
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddPolygon(polygon);
                return path.IsVisible(new Point(pointX, pointY));
            }
        }
    }
}
