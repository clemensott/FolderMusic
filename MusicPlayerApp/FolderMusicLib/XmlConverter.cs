using System;
using System.IO;
using System.Xml.Serialization;

namespace MusicPlayer
{
    public class XmlConverter
    {
        public static T Deserialize<T>(string xmlText)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));

            TextReader tr = new StringReader(xmlText);
            object deObj = serializer.Deserialize(tr);

            return (T)deObj;
        }

        public static string Serialize(object obj)
        {
            Type type = obj.GetType();

            try
            {
                XmlSerializer serializer = new XmlSerializer(type);

                TextWriter tw = new StringWriter();
                serializer.Serialize(tw, obj);

                return tw.ToString();
            }
            catch { }

            return string.Empty;
        }
    }
}
