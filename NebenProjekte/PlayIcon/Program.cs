using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayIcon
{
    class Program
    {
        const int a = 1000, t = 50, w = 300, f = 10;

        static void Main(string[] args)
        {
            using (Bitmap bmp = new Bitmap(a * f, a * f))
            {
                Graphics g = Graphics.FromImage(bmp);

                g.DrawEllipse(new Pen(Color.Black, t * f), t / 2 * f, t / 2 * f, (a - t) * f, (a - t) * f);

                Point[] triangulum = new Point[3];

                triangulum[0] = GetPoint(0, w - 100);
                triangulum[1] = GetPoint(120, w);
                triangulum[2] = GetPoint(240, w);

                g.FillPolygon(Brushes.Black, triangulum);

                Bitmap bmpOut = new Bitmap(bmp, 500, 500);
                string path = string.Format(@"C:\Users\Clemens\Desktop\PlayLogo.bmp");

                bmpOut.Save(path);
            }
        }

        private static Point GetPoint(int angle, int w)
        {
            int x, y;
            double xYRatio = Math.Tan(angle * Math.PI / 180);

            x = Convert.ToInt32(Math.Sqrt(4 * w * w * (xYRatio * xYRatio + 1)) / (2 * xYRatio * xYRatio + 2));

            if (angle > 90 && angle < 270) x *= -1;

            y = Convert.ToInt32(x * xYRatio);

            return new Point((a / 2 + x) * f, (a / 2 + y) * f);
        }
    }
}
