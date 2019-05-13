using System;
using System.Drawing;

namespace ShuffleIcon
{
    class Program
    {
        private const int f = 35, t = 25, a = 250, c = 190, h = 180;
        private const double s = 1.0393;
        private static readonly Color deleteColor = Color.Red;

        private static readonly IconColor[] colors = new IconColor[]
        {
            new IconColor( "Black", Color.Black),
            new IconColor( "White", Color.White),
            new IconColor( "Blue", Color.Blue),
            new IconColor( "Yellow", Color.Yellow),
            new IconColor( "Gray", Color.Gray),
            new IconColor( "LightGray", Color.LightGray)
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

            Bitmap bmp = new Bitmap(a * f, a * f);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                Bitmap bmpSquare = GetSquareIcon(a, c, color);
                Bitmap bmpNotSquare = new Bitmap(bmpSquare, a * f, h * f);

                g.DrawImage(bmpNotSquare, 0 * f, (a - h) / 2 * f);

                bmpSquare.Dispose();
                bmpNotSquare.Dispose();
            }

            bmp = new Bitmap(bmp, new Size(a, a));
            bmp.Save(string.Format("ShuffleIcon{0}.png", color.Name), System.Drawing.Imaging.ImageFormat.Png);
        }

        private static Bitmap GetSquareIcon(int a, int crossSize, IconColor color)
        {
            int arrowMiddleY;
            Bitmap bmp = new Bitmap(a * f, a * f);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                int yCrossOffset = (a - crossSize - t) / 2;
                Bitmap bmpCross = GetCross(crossSize, color, out arrowMiddleY);
                g.DrawImage(bmpCross, 0 * f, yCrossOffset * f);

                bmpCross.Dispose();

                Point[] arrowUpPoints = new Point[3];
                Point[] arrowDownPoints = new Point[3];

                arrowUpPoints[0] = new Point(crossSize * f, 0 * f);
                arrowUpPoints[1] = new Point(crossSize * f, (yCrossOffset + arrowMiddleY) * 2 * f);
                arrowUpPoints[2] = new Point(a * f, (yCrossOffset + arrowMiddleY) * f);

                arrowDownPoints[0] = new Point(crossSize * f, a * f);
                arrowDownPoints[1] = new Point(crossSize * f, (a - (arrowMiddleY + yCrossOffset) * 2) * f);
                arrowDownPoints[2] = new Point(a * f, (a - arrowMiddleY - yCrossOffset) * f);

                g.FillPolygon(color.Brush, arrowDownPoints);
                g.FillPolygon(color.Brush, arrowUpPoints);
            }

            return bmp;
        }

        private static Bitmap GetCross(int a, IconColor color, out int yEnd)
        {
            int w = a, h = a + t;
            Bitmap bmp = new Bitmap(w * f, h * f);

            yEnd = 0;

            using (Graphics g = Graphics.FromImage(bmp))
            {
                double middleX = MiddleX(w, s);
                Point[] pointsDown = new Point[w + t];
                Point[] pointsUp = new Point[w + t];
                double calcX = middleX - pointsDown.Length / 2.0;
                int calcCount = (int)Math.Ceiling(pointsDown.Length / 2.0);

                for (int i = 0; i < calcCount; i++, calcX++)
                {
                    double yDown = Y(w, s, calcX) + t / 2.0;
                    double yUp = h - yDown;
                    int actualX = i - t / 2;

                    pointsDown[i] = new Point(actualX * f, (int)(yDown * f));
                    pointsUp[pointsDown.Length - 1 - i] = new Point((w - actualX) * f, (int)(yDown * f));
                    pointsUp[i] = new Point(actualX * f, (int)(yUp * f));
                    pointsDown[pointsDown.Length - 1 - i] = new Point((w - actualX) * f, (int)(yUp * f));

                    if (Math.Abs(actualX) < 0.6)
                    {
                        yEnd = (int)yDown;
                    }
                }

                g.DrawLines(new Pen(color.Color, t * f), pointsDown);

                g.DrawLines(new Pen(deleteColor, t * 2 * f), pointsUp);
                g.DrawLines(new Pen(color.Color, t * f), pointsUp);

            }
            // /*
            for (int i = 0; i < bmp.Width; i++)
            {
                for (int j = 0; j < bmp.Height; j++)
                {
                    Color pixel = bmp.GetPixel(i, j);

                    if (pixel.A == deleteColor.A && pixel.B == deleteColor.B &&
                        pixel.G == deleteColor.G && pixel.R == deleteColor.R)
                    {
                        bmp.SetPixel(i, j, Color.Transparent);
                    }
                }

                if (i % 100 == 0) Console.WriteLine("i: {0}", i);
            }       //          */

            return bmp;
        }

        private static double Y(double b, double s, double x)
        {
            return b * Math.Pow(s, x) / (Math.Pow(s, x) + b - 1);
        }

        private static double MiddleX(int b, double s)
        {
            bool isLower = true;
            double x = 0, y = 0, step = 100, half = b / 2.0;

            do
            {
                if (y < half)
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

            } while (Math.Abs(y - half) > 0.01 && step > 0);

            return x;
        }
    }
}
