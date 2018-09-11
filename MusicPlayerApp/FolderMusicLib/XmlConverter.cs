using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace MusicPlayer
{
    public class XmlConverter
    {
        public static XmlReader GetReader(string xmlText)
        {
            XmlReader reader = XmlReader.Create(new StringReader(xmlText));
            reader.MoveToContent();

            return reader;
        }

        public static T Deserialize<T>(string xmlText)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));

            TextReader tr = new StringReader(xmlText);
            object deObj = serializer.Deserialize(tr);

            return (T)deObj;
        }

        public static string Serialize(object obj)
        {
            TextWriter tw = new StringWriter();

            try
            {
                Type objType = obj.GetType();
                IXmlSerializable serializableObj = obj as IXmlSerializable;

                if (serializableObj != null)
                {
                    using (XmlWriter writer = XmlWriter.Create(tw))
                    {
                        writer.WriteStartDocument();
                        writer.WriteStartElement(objType.Name);
                        serializableObj.WriteXml(writer);
                        writer.WriteEndElement();
                        writer.WriteEndDocument();
                    }
                }
                else
                {
                    XmlSerializer serializer = new XmlSerializer(objType);
                    serializer.Serialize(tw, obj);
                }
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("XmlSerializeFail", e);
            }

            return tw.ToString();
        }
    }
}
