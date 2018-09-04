using System;
using System.IO;
using System.Xml.Serialization;

namespace MusicPlayerLib
{
    public abstract class XmlConverter
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
                string xmlText = tw.ToString();

                TextReader tr = new StringReader(xmlText);
                object deObj = serializer.Deserialize(tr);

                return deObj.GetType() == type ? xmlText : "";
            }
            catch { }

            return "";
        }
    }
}
