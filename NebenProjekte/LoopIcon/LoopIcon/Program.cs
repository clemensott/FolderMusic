using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoopIcon
{
    class Program
    {
        private const bool withOne = true;
        private const int f = 35, a = 250, t = 25, aL = 40, oS = 150, oX = 100, oY = -20;

        // White (withOne/!withOne), GrayForLight (!withOne), GrayForDark (!withOne)
        private static readonly string colorName = "White";
        private static readonly Color color = Color.White;
        private static readonly Brush brush = Brushes.White;

        //private static readonly string colorName = "DarkGray";
        //private static readonly Color color = Color.Gray;
        //private static readonly Brush brush = Brushes.Gray;

        //private static readonly string colorName = "LightGray";
        //private static readonly Color color = Color.LightGray;
        //private static readonly Brush brush = Brushes.LightGray;

        private static readonly Pen pen = new Pen(color, t * f);

        static void Main(string[] args)
        {
            foreach (FontFamily ff in FontFamily.Families)
            {
                Console.WriteLine(ff.ToString());
            }

            Bitmap bmp = new Bitmap(a * f, a * f);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                int startAngle = withOne ? 45 : -45, sweepAngle = withOne ? 185 : 275;
                int arrowPointXY = Convert.ToInt32(1.0 * (a - (a - t) / Math.Sqrt(2)) / 2);
                int t45DegreesXY = Convert.ToInt32(t * 1.5 / Math.Sqrt(2));
                Point[] arrowPoints = new Point[3];

                arrowPoints[0] = new Point((arrowPointXY - t45DegreesXY) * f, (arrowPointXY - t45DegreesXY) * f);
                arrowPoints[1] = new Point((arrowPointXY + t45DegreesXY) * f, (arrowPointXY + t45DegreesXY) * f);
                arrowPoints[2] = new Point((arrowPointXY + aL) * f, (arrowPointXY - aL) * f);

                g.FillPolygon(brush, arrowPoints);
                g.DrawArc(pen, t / 2 * f, t / 2 * f, (a - t) * f, (a - t) * f, startAngle, sweepAngle);

                if (withOne)
                {
                    g.DrawString("1", new Font(FontFamily.Families[0], oS * f), brush, new PointF(oX * f, oY * f));
                }
            }

            bmp = new Bitmap(bmp, a, a);
            bmp.Save("Test.bmp");
        }
    }
}
