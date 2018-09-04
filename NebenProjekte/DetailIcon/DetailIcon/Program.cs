using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DetailIcon
{
    class Program
    {
        const int a = 1000, t = 50;

        static void Main(string[] args)
        {
            int f = 10;

            using (Bitmap bmp = new Bitmap(a*f, a*f))
            {
                Graphics g = Graphics.FromImage(bmp);

                g.DrawEllipse(new Pen(Color.Black, t * f), t / 2 * f, t / 2 * f, (a - t) * f, (a - t) * f);

                //    g.FillRectangle(Brushes.Black, new Rectangle(200, 250, 100, 100));
                //  g.FillRectangle(Brushes.Black, new Rectangle(200, 450, 100, 100));
                //g.FillRectangle(Brushes.Black, new Rectangle(200, 650, 100, 100));

                g.FillRectangle(Brushes.Black, new Rectangle(200 * f, 275 * f, 600 * f, 50 * f));
                g.FillRectangle(Brushes.Black, new Rectangle(200 * f, 475 * f, 600 * f, 50 * f));
                g.FillRectangle(Brushes.Black, new Rectangle(200 * f, 675 * f, 600 * f, 50 * f));

                Bitmap bmpOut = new Bitmap(bmp, 500, 500);
                string path = string.Format(@"C:\Users\Clemens\Desktop\DetailIcon.bmp");

                bmpOut.Save(path);
            }
        }
    }
}
