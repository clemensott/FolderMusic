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
            obj.ReadXml(GetReader(xmlText));

            return obj;
        }

        public static T DeserializeNew<T>(string xmlText) where T : IXmlSerializable, new()
        {
            T obj = new T();
            obj.ReadXml(GetReader(xmlText));

            return obj;
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
                    MobileDebug.Service.WriteEventPair("XmlReadListFail", e, "Type: ", typeof(T).FullName,
                        "Name: ", reader.Name, "Node: ", reader.NodeType, "UntilName: ", elementName);

                    yield break;
                }

                yield return item;
            }
        }

        public static T Deserialize<T>(string xmlText)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            TextReader tr = new StringReader(xmlText);
            if ((typeof(T) == typeof(Data.Song[]))) MobileDebug.Service.WriteEvent("DeserializeSong[]1");
            try
            {
                object deObj = serializer.Deserialize(tr);
                if ((typeof(T) == typeof(Data.Song[]))) MobileDebug.Service.WriteEvent("DeserializeSong[]2", deObj?.GetType()?.Name);
                return (T)deObj;
            }
            catch (Exception e)
            {
                MobileDebug.Service.WriteEvent("DeserializeFail", e);
                throw;
            }

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
