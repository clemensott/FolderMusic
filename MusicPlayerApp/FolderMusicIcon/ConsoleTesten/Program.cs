using System.Drawing;

namespace ConsoleTesten
{
    class Program
    {
        static void Main(string[] args)
        {
            int f = 50, width, height = 360;
            int xb = 25, yb = 25, xg = (92 + xb * 2) * f, yg = (92 + yb * 2) * f;
            Brush brush = Brushes.White;

            width = height * xg / yg;

            using (Bitmap bmp = new Bitmap(xg, yg))
            {
                Graphics g = Graphics.FromImage(bmp);

                //Oben
                Point[] obenPoints = new Point[] { new Point((12 + xb) * f, (0 + yb) * f),
                    new Point((32 + xb) * f, (0 + yb) * f), new Point((36 + xb) * f, (8 + yb) * f),
                    new Point((8 + xb) * f, (8 + yb) * f) };
                g.FillPolygon(brush, obenPoints);

                //Ordner links oben
                Point[] ordnerPoints1 = new Point[] { new Point((4 + xb) * f, (10 + yb) * f),
                    new Point((70 + xb) * f, (10 + yb) * f), new Point((74 + xb) * f, (14 + yb) * f),
                    new Point((74 + xb) * f, (20 + yb) * f), new Point((41 + xb) * f, (34 + yb) * f),
                    new Point((41 + xb) * f, (54 + yb) * f), new Point((4 + xb) * f, (54 + yb) * f),
                    new Point((0 + xb) * f, (48 + yb) * f), new Point((0 + xb) * f, (14 + yb) * f) };
                g.FillPolygon(brush, ordnerPoints1);

                //Ordner rechts unten
                Point[] ordnerPoints2 = new Point[] { new Point((74 + xb) * f, (33 + yb) * f),
                    new Point((74 + xb) * f, (50 + yb) * f), new Point((70 + xb) * f, (54 + yb) * f),
                    new Point((53 + xb) * f, (54 + yb) * f), new Point((53 + xb) * f, (41 + yb) * f) };
                g.FillPolygon(brush, ordnerPoints2);

                //Note gerüst
                Point[] noteGerüstPoints = new Point[] { new Point((44 + xb) * f, (76 + yb) * f),
                    new Point((44 + xb) * f, (36 + yb) * f), new Point((92 + xb) * f, (16 + yb) * f),
                    new Point((92 + xb) * f, (70 + yb) * f), new Point((79 + xb) * f, (62 + yb) * f),
                    new Point((86 + xb) * f, (62 + yb) * f), new Point((86 + xb) * f, (25 + yb) * f),
                    new Point((50 + xb) * f, (39 + yb) * f), new Point((50 + xb) * f, (84 + yb) * f),
                    new Point((37 + xb) * f, (76 + yb) * f) };
                g.FillPolygon(brush, noteGerüstPoints);

                //Noten Kreis links
                Rectangle notenKreis1 = new Rectangle((24 + xb) * f, (76 + yb) * f, 26 * f, 16 * f);
                g.FillEllipse(brush, notenKreis1);

                //Noten Kreis rechts
                Rectangle notenKreis2 = new Rectangle((66 + xb) * f, (62 + yb) * f, 26 * f, 16 * f);
                g.FillEllipse(brush, notenKreis2);

                //Ecke links oben
                Rectangle eckeRechtesOben = new Rectangle((0 + xb) * f, (10 + yb) * f, 8 * f, 8 * f);
                g.FillEllipse(brush, eckeRechtesOben);

                //Ecke rechts oben
                Rectangle eckeLinksOben = new Rectangle((66 + xb) * f, (10 + yb) * f, 8 * f, 8 * f);
                g.FillEllipse(brush, eckeLinksOben);

                //Ecke rechts unten
                Rectangle eckeLinksUnten = new Rectangle((66 + xb) * f, (46 + yb) * f, 8 * f, 8 * f);
                g.FillEllipse(brush, eckeLinksUnten);

                //Ecke links unten
                Rectangle eckeRechtsUnten = new Rectangle((0 + xb) * f, (46 + yb) * f, 8 * f, 8 * f);
                g.FillEllipse(brush, eckeRechtsUnten);

                string path = string.Format(@"G:\Users\Clemens\Desktop\logo {0}x{1}.bmp", width, height);
                new Bitmap(bmp, width, height).Save(path);
                //System.Diagnostics.Process.Start(path);
            }
        }
    }
}
