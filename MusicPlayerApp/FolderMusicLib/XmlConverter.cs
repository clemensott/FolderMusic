using System;
using System.Collections.Generic;
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

        public static T Deserialize<T>(T obj, string xmlText) where T : IXmlSerializable
        {
            //if (obj is Data.IPlaylist) MobileDebug.Service.WriteEvent("Deserilize", typeof(T).FullName, xmlText);
            obj.ReadXml(GetReader(xmlText));

            return obj;
        }

        public static T DeserializeNew<T>(string xmlText) where T : IXmlSerializable, new()
        {
            T obj = new T();
            obj.ReadXml(GetReader(xmlText));

            return obj;
        }

        public static IEnumerable<T> DeserializeList<T>(string xmlText, string elementName = null) where T : IXmlSerializable, new()
        {
            XmlReader reader = GetReader(xmlText);
            MobileDebug.Service.WriteEvent("DeserializeList1", reader.Name);
            reader.ReadStartElement();
            MobileDebug.Service.WriteEvent("DeserializeList2", reader.Name);
            return DeserializeList<T>(reader, elementName);
        }

        public static IEnumerable<T> DeserializeList<T>(XmlReader reader, string elementName = null) where T : IXmlSerializable, new()
        {
            while (reader.NodeType == XmlNodeType.Element)
            {
                if (elementName != null && reader.Name != elementName) yield break;

                T item;

                try
                {
                    item = Deserialize(new T(), reader.ReadOuterXml());
                }
                catch (Exception e)
                {
                    MobileDebug.Service.WriteEventPair("XmlReadListFail", "Type", typeof(T).FullName,
                        "Name", reader.Name, "Node", reader.NodeType, "UntilName", elementName, e);

                    yield break;
                }

                yield return item;
            }
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

        public static string SerializeList<T>(IEnumerable<T> list) where T : IXmlSerializable
        {
            TextWriter tw = new StringWriter();

            using (XmlWriter writer = XmlWriter.Create(tw))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement(typeof(T).Name + "s");

                foreach (T item in list)
                {
                    writer.WriteStartElement(typeof(T).Name);
                    item.WriteXml(writer);
                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }

            return tw.ToString();
        }
    }
}
