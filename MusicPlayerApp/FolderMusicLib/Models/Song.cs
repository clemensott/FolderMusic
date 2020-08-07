using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace MusicPlayer.Models
{
    public struct Song : IXmlSerializable
    {
        private string artist;

        public long DurationTicks
        {
            get { return Duration.Ticks; }
            set { Duration = TimeSpan.FromTicks(value); }
        }

        [XmlIgnore] public TimeSpan Duration { get; set; }

        public string Title { get; set; }

        public string Artist
        {
            get { return string.IsNullOrWhiteSpace(artist) ? "Unknown" : artist; }
            set { artist = value; }
        }

        public string FullPath { get; set; }

        public override string ToString()
        {
            return !string.IsNullOrEmpty(Artist) ? Artist + " - " + Title : Title;
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            DurationTicks = long.Parse(reader.GetAttribute(nameof(DurationTicks)));
            Title = reader.GetAttribute(nameof(Title));
            Artist = reader.GetAttribute(nameof(Artist));
            FullPath = reader.GetAttribute(nameof(FullPath));

            reader.ReadStartElement();
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString(nameof(DurationTicks), DurationTicks.ToString());
            writer.WriteAttributeString(nameof(Title), Title);
            writer.WriteAttributeString(nameof(Artist), Artist);
            writer.WriteAttributeString(nameof(FullPath), FullPath);
        }
    }
}
