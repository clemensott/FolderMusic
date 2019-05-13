using System.Drawing;

namespace ShuffleIcon
{
    struct IconColor
    {
        public string Name { get; }

        public Color Color { get; }

        public Brush Brush { get; }

        public IconColor(string name, Color color)
        {
            Name = name;
            Color = color;
            Brush = new SolidBrush(color);
        }
    }
}
