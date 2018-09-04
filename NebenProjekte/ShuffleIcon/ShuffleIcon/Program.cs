using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShuffleIcon
{
    class Program
    {
        private const int f = 35, t = 25, a = 250, c = 200, h = 180;
        private const double s = 1.009;
        private static readonly Color deleteColor = Color.Red;

        // Colors: Blue, Orange, Black, White, GrayForLight, GrayForDark
        private static readonly string colorName = "Blue";
        private static readonly Color drawColor = Color.Blue;
        private static readonly Brush drawBrush = Brushes.Blue;

        //private static readonly string colorName = "Orange";
        //private static readonly Color drawColor = Color.Orange;
        //private static readonly Brush drawBrush = Brushes.Orange;

        //private static readonly string colorName = "Black";
        //private static readonly Color drawColor = Color.Black;
        //private static readonly Brush drawBrush = Brushes.Black;

        //private static readonly string colorName = "White";
        //private static readonly Color drawColor = Color.White;
        //private static readonly Brush drawBrush = Brushes.White;

        //private static readonly string colorName = "DarkGray";
        //private static readonly Color drawColor = Color.Gray;
        //private static readonly Brush drawBrush = Brushes.Gray;

        //private static readonly string colorName = "LightGray";
        //private static readonly Color drawColor = Color.LightGray;
        //private static readonly Brush drawBrush = Brushes.LightGray;


        static void Main(string[] args)
        {
            Bitmap bmp = new Bitmap(a * f, a * f);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                Bitmap bmpSquare = GetSquareIcon(a, c);
                Bitmap bmpNotSquare = new Bitmap(bmpSquare, a * f, h * f);

                g.DrawImage(bmpNotSquare, 0 * f, (a - h) / 2 * f);

                bmpSquare.Dispose();
                bmpNotSquare.Dispose();
            }

            bmp = new Bitmap(bmp, new Size(a, a));
            bmp.Save(string.Format("ShuffleIcon{0}.png", colorName), System.Drawing.Imaging.ImageFormat.Png);
        }

        private static Bitmap GetSquareIcon(int a, int crossSize)
        {
            int arrowMiddleY;
            Bitmap bmp = new Bitmap(a * f, a * f);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                Bitmap bmpCross = GetCross(crossSize, out arrowMiddleY);
                g.DrawImage(bmpCross, new Point(0 * f, 50 * f));

                bmpCross.Dispose();

                Point[] arrowUpPoints = new Point[3];
                Point[] arrowDownPoints = new Point[3];

                arrowUpPoints[0] = new Point(crossSize * f, 0 * f);
                arrowUpPoints[1] = new Point(crossSize * f, arrowMiddleY * 2 * f);
                arrowUpPoints[2] = new Point(a * f, arrowMiddleY * f);

                arrowDownPoints[0] = new Point(crossSize * f, a * f);
                arrowDownPoints[1] = new Point(crossSize * f, (a - arrowMiddleY * 2) * f);
                arrowDownPoints[2] = new Point(a * f, (a - arrowMiddleY) * f);

                g.FillPolygon(drawBrush, arrowDownPoints);
                g.FillPolygon(drawBrush, arrowUpPoints);
            }

            return bmp;
        }

        private static Bitmap GetCross(int a, out int yEnd)
        {
            int w = a, h = a + t;
            Bitmap bmp = new Bitmap(w * f, h * f);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                int middleX, iWhenXIsW = 0;

                middleX = MiddleX(w, s);

                Point[] pointsDown = new Point[w + t];
                Point[] pointsUp = new Point[w + t];

                for (int i = 0, x = middleX - w / 2 - 50; i < pointsDown.Length; i++)
                {
                    int yDown = (Y(w - t, s, x++) + t);
                    pointsDown[i] = new Point(i * f, yDown * f);
                    pointsUp[i] = new Point(i * f, (h - yDown) * f);

                    if (x == w) iWhenXIsW = i;
                }

                g.DrawLines(new Pen(drawColor, t * f), pointsDown);

                g.DrawLines(new Pen(deleteColor, t * 2 * f), pointsUp);
                g.DrawLines(new Pen(drawColor, t * f), pointsUp);

                yEnd = Y(w - t, s, iWhenXIsW) + t;
            }

            for (int i = 0; i < bmp.Width; i++)
            {
                for (int j = 0; j < bmp.Height; j++)
                {
                    Color color = bmp.GetPixel(i, j);

                    if (color.A == deleteColor.A && color.B == deleteColor.B &&
                        color.G == deleteColor.G && color.R == deleteColor.R)
                    {
                        bmp.SetPixel(i, j, Color.Transparent);
                    }
                }

                if (i % 100 == 0) Console.WriteLine("i: {0}", i);
            }       //          */

            return bmp;
        }

        private static int Y(int b, double s, int x)
        {
            return Convert.ToInt32((b * Math.Pow(s, x)) / (Math.Pow(s, x) + b - 1));
        }

        private static int MiddleX(int b, double s)
        {
            bool isLower = true;
            int x = 0, y = 0, step = 100;

            do
            {
                if (y < b / 2)
                {
                    if (!isLower) step /= 10;

                    isLower = true;
                    x += step;
                }
                else
                {
                    if (isLower) step /= 10;

                    isLower = false;
                    x -= step;
                }

                y = Y(b, s, x);

            } while (y != b / 2 && step > 0);

            return x;
        }
    }
}
