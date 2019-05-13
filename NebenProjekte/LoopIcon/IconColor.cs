using System.Drawing;

namespace LoopIcon
{
    struct IconColor
    {
        public string Name { get; }

        public Color Color { get; }

        public Brush Brush { get; }

        public bool WithOne { get; }

        public IconColor(string name, Color color, bool withOne)
        {
            Name = name;
            Color = color;
            Brush = new SolidBrush(Color);
            WithOne = withOne;
        }
    }
}
