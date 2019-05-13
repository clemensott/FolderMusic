using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoopIcon
{
    class Program
    {
        private const int f = 35, a = 250, t = 25, aL = 40, oS = 150, oX = 100, oY = -20;

        private static IconColor[] colors = new IconColor[]
        {
            new IconColor("Black", Color.Black,false),
            new IconColor("White", Color.White,false),
            new IconColor("LightGray", Color.LightGray,false),
            new IconColor("DarkGray", Color.DarkGray,false),
            new IconColor("Black", Color.Black,true),
            new IconColor("White", Color.White,true)
        };

        static void Main(string[] args)
        {
            foreach (IconColor color in colors)
            {
                GenerateIcon(color);
            }
        }

        private static void GenerateIcon(IconColor color)
        {
            Console.WriteLine(color.Name);

            FontFamily ff = FontFamily.Families[3];
            Bitmap bmp = new Bitmap(a * f, a * f);
            Pen pen = new Pen(color.Color, t * f);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                int startAngle = color.WithOne ? 45 : -45, sweepAngle = color.WithOne ? 185 : 275;
                int arrowPointXY = Convert.ToInt32(1.0 * (a - (a - t) / Math.Sqrt(2)) / 2);
                int t45DegreesXY = Convert.ToInt32(t * 1.5 / Math.Sqrt(2));
                Point[] arrowPoints = new Point[3];

                arrowPoints[0] = new Point((arrowPointXY - t45DegreesXY) * f, (arrowPointXY - t45DegreesXY) * f);
                arrowPoints[1] = new Point((arrowPointXY + t45DegreesXY) * f, (arrowPointXY + t45DegreesXY) * f);
                arrowPoints[2] = new Point((arrowPointXY + aL) * f, (arrowPointXY - aL) * f);

                g.FillPolygon(color.Brush, arrowPoints);
                g.DrawArc(pen, t / 2 * f, t / 2 * f, (a - t) * f, (a - t) * f, startAngle, sweepAngle);

                if (color.WithOne)
                {
                    g.DrawString("1", new Font(ff, oS * f), color.Brush, new PointF(oX * f, oY * f));
                }
            }

            bmp = new Bitmap(bmp, a, a);
            bmp.Save(string.Format("LoopIcon{0}{1}.png", color.Name,color.WithOne), ImageFormat.Png);
        }
    }
}
