using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Xml.Serialization;
using System.IO.Compression;
using System.Windows.Media;
using System.Drawing;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Xml;
using System.Xml.Schema;
using System.Diagnostics;
using System.Reflection;

namespace ConsoleTesten
{
    public class Program
    {
        private const string path1 = @"D:\MusicPlayerApp", path2 = @"D:\Projects\FolderMusic\MusicPlayerApp";

        private static void Folder(string path)
        {
            foreach (string file in Directory.GetFiles(path).Where(f => Path.GetExtension(f) == ".cs"))
            {
                Compare(file.Remove(0, path1.Length));
            }

            foreach (string file in Directory.GetFiles(path).Where(f => Path.GetExtension(f) == ".xaml"))
            {
                Compare(file.Remove(0, path1.Length));
            }

            foreach (string directory in Directory.GetDirectories(path))
            {
                Folder(directory);
            }
        }

        private static void Compare(string prePath)
        {

            string text1, text2;

            try
            {
                text1 = File.ReadAllText(path1 + prePath);
            }
            catch
            {
                text1 = string.Empty;
                Console.WriteLine("Can't read: " + path1 + prePath);
            }

            try
            {
                text2 = File.ReadAllText(path2 + prePath);
            }
            catch
            {
                text2 = string.Empty;
                Console.WriteLine("Can't read: " + path2 + prePath);
            }
            bool same = text1 == text2;
            if (text1 == text2) return;

            Console.WriteLine("Diffrent: " + prePath);
        }

        private static void Print(params object[] data)
        {
            Print(data.AsEnumerable());
        }

        private static void Print(IEnumerable data)
        {
            foreach (string line in data.Cast<object>().Select(x => x?.ToString() ?? "nullRef").ToArray()) Console.WriteLine(line);
        }

        static void Main(string[] args)
        {
            //Folder(path1);
            //Console.WriteLine("Fertig...");
            //Console.ReadLine();

            Print("hsadkjf", null, null, "kljasdhfsdl", 4, null, 4, 67, null, "kklsdhf", null, null, null);
            return;

            var l = new List<string>
            {
                "dkjsad",
                "shfdliuarhsf",
                "rdgf",
                "öljewnc",
                "shfdliucwiarhsf",
                "lw jceaofawe",
                "s98dzbuf9cw",
                "98 43wf 8",
                "p 98q4hwfn"
            };

            TextWriter tw = new StringWriter();
            using (XmlWriter writer = XmlWriter.Create(tw))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("F");
                F.GetDefault().WriteXml(writer);
                writer.WriteEndElement();
                writer.WriteEndDocument();
            }

            string xml = tw.ToString();
            TextReader tr = new StringReader(tw.ToString());
            using (XmlReader reader = XmlReader.Create(tr))
            {
                reader.MoveToContent();
                F e = new F(reader);
            }

            using (Stream stream = new FileStream("Test.xml", FileMode.Create))
            {
                object obj = E.GetDefault();
                bool isType = obj.GetType().GetTypeInfo().IsAssignableFrom(typeof(IXmlSerializable));
                XmlSerializer serializer = new XmlSerializer(obj.GetType());

                serializer.Serialize(stream, obj);
            }

            using (Stream stream = new FileStream("Test.xml", FileMode.Open))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(E));

                var obj = serializer.Deserialize(stream);
            }

            Console.WriteLine("Fertig...");
            Console.ReadLine();
        }
    }

    static class AEx
    {
        public static string GetText(this A a)
        {
            if (a == null) return "null";

            return "notNull";
        }
    }

    public class A : IXmlSerializable
    {
        public double No1 { get; set; }

        public double No2 { get; set; }

        public string Text1 { get; set; }

        public string Text2 { get; set; }

        public List<int> List1 { get; set; }

        public List<Point> List2 { get; set; }

        public Rule RuleProp { get; set; }

        public DateTime Time { get; set; }

        private A() { }

        public A(XmlReader reader)
        {
            ReadXml(reader);
        }

        public static A GetDefault()
        {
            return new A()
            {
                No1 = 384.847,
                No2 = -0.84273659,

                Text1 = "Hallo Welt!",
                Text2 = "ldsauhfas",

                List1 = new List<int>() { 92874, 45, 645, 7, 245, 7, 42, 724, 5, 74577, 4345, 743, 57, 43, 5734 },
                List2 = new List<Point>() { new Point(8734, 4572), new Point(4873, 984761), new Point(45, 9), new Point(1, 436374591) },

                RuleProp = Rule.SetDefault,

                Time = new DateTime(448732984234543298)
            };
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            No1 = double.Parse(reader.GetAttribute("No1"));
            No2 = double.Parse(reader.GetAttribute("No2"));

            Text1 = reader.GetAttribute("Text1");
            Text2 = reader.GetAttribute("Text2");

            RuleProp = (Rule)Enum.Parse(typeof(Rule), reader.GetAttribute("RuleProp"));
            Time = new DateTime(long.Parse(reader.GetAttribute("Time")));
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("No1", No1.ToString());
            writer.WriteAttributeString("No2", No2.ToString());

            writer.WriteAttributeString("Text1", Text1);
            writer.WriteAttributeString("Text2", Text2);

            writer.WriteAttributeString("RuleProp", RuleProp.ToString());
            writer.WriteAttributeString("Time", Time.Ticks.ToString());

            //writer.WriteStartElement("List1");
            //writer.WriteEndElement();
            //writer.WriteElementString("List1", "List1Value");
            //writer.WriteElementString("List2", "List1Value");
        }
    }

    public class B : ObservableCollection<A>, IXmlSerializable
    {
        private B() { }

        public B(XmlReader reader)
        {
            ReadXml(reader);
        }

        public static B GetDefault()
        {
            return new B() { A.GetDefault(), A.GetDefault(), A.GetDefault() };
        }

        protected override void MoveItem(int oldIndex, int newIndex)
        {
            //if (string.Compare(this[oldIndex], this[newIndex]) < 0)
            //    Move(newIndex, oldIndex);
        }

        protected override void RemoveItem(int index)
        {
            Console.WriteLine(this[index]);
        }

        protected override void ClearItems()
        {
            //base.ClearItems();
        }

        //protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        //{
        //    System.Diagnostics.Debug.WriteLine(e.Action.ToString() + ";New: " +
        //        (e.NewItems?.Count.ToString() ?? "0") + ";Old: " + (e.OldItems?.Count.ToString() ?? "0"));
        //}

        public string GetOtherValue()
        {
            return "BO";
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            reader.ReadStartElement();

            while (reader.NodeType == XmlNodeType.Element)
            {
                //A value = new A(reader);
                ////value.ReadXml(reader);
                //this.Add(value);
                string inner = reader.ReadOuterXml();
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            foreach (A a in this)
            {
                writer.WriteStartElement("A");
                a.WriteXml(writer);
                writer.WriteEndElement();
            }
        }
    }

    public interface IC : IXmlSerializable
    {
        double No1 { get; set; }

        double No2 { get; set; }

        string Text1 { get; set; }

        string Text2 { get; set; }

        B Bs { get; set; }

        F Fs { get; set; }
    }

    class C : IC
    {
        public double No1 { get; set; }

        public double No2 { get; set; }

        public string Text1 { get; set; }

        public string Text2 { get; set; }

        public B Bs { get; set; }

        public F Fs { get; set; }

        private C()
        {
        }

        public C(XmlReader reader)
        {
            ReadXml(reader);
        }

        public static C GetDefault()
        {
            return new C()
            {
                No1 = 39874,
                No2 = 73.908345,
                Text1 = "Name",
                Text2 = "Count",
                Bs = B.GetDefault(),
                Fs = F.GetDefault()
            };
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            No1 = double.Parse(reader.GetAttribute("No1"));
            No2 = double.Parse(reader.GetAttribute("No2"));
            Text1 = reader.GetAttribute("Text1");
            Text2 = reader.GetAttribute("Text2");

            //reader.ReadStartElement();
            //string inner = reader.ReadOuterXml();
            //string outer = reader.ReadOuterXml();
            //XmlReader innerReader = XmlReader.Create(new StringReader(inner));
            //innerReader.MoveToContent();
            //return;
            reader.ReadStartElement();
            Bs = new B(reader);
            //Bs.ReadXml(reader);
            reader.ReadEndElement();

            reader.ReadStartElement();
            Fs = new F();
            Fs.ReadXml(reader);
            reader.ReadEndElement();
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("No1", No1.ToString());
            writer.WriteAttributeString("No2", No2.ToString());
            writer.WriteAttributeString("Text1", Text1);
            writer.WriteAttributeString("Text2", Text2);

            writer.WriteStartElement("Bs");
            Bs.WriteXml(writer);
            writer.WriteEndElement();

            writer.WriteStartElement("Fs");
            Fs.WriteXml(writer);
            writer.WriteEndElement();
        }
    }

    public interface ID : IEnumerable<IC>, IXmlSerializable
    {

    }

    class D : ID
    {
        private List<IC> list;

        public D()
        {
            list = new List<IC>();
        }

        public D(IEnumerable<C> items)
        {
            list = new List<IC>(items);
        }

        public static D GetDefault()
        {
            return new D(new List<C>() { C.GetDefault(), C.GetDefault(), C.GetDefault(), C.GetDefault() });
        }

        public IEnumerator<IC> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            reader.ReadStartElement();

            while (reader.NodeType == XmlNodeType.Element)
            {
                C item = new C(reader);
                //item.ReadXml(reader);

                list.Add(item);

                reader.Read();
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("Type", "Test");
            foreach (var item in this)
            {
                writer.WriteStartElement("C");
                item.WriteXml(writer);
                writer.WriteEndElement();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return list.GetEnumerator();
        }
    }

    public class E : IXmlSerializable
    {
        public int Index { get; set; }

        public ID Ds { get; set; }

        public E()
        {
            Index = -1;
            Ds = new D();
        }

        public E(XmlReader reader)
        {
            ReadXml(reader);
        }

        public static E GetDefault()
        {
            return new E()
            {
                Index = 2,
                Ds = D.GetDefault()
            };
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            Index = int.Parse(reader.GetAttribute("Index"));

            reader.ReadStartElement();
            Ds = new D();
            Ds.ReadXml(reader);
            reader.ReadEndElement();
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteAttributeString("Index", Index.ToString());

            writer.WriteStartElement("Ds");
            Ds.WriteXml(writer);
            writer.WriteEndElement();
        }
    }

    public class F : List<string>, IXmlSerializable
    {
        public F() { }

        public F(XmlReader reader)
        {
            ReadXml(reader);
        }

        public static F GetDefault()
        {
            return new F() { "34q c5z347tq3", "4u r084zht 9q", "skajh 45978q}", " y 9vua 9w84t" };
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            reader.ReadStartElement();
            while (reader.NodeType == XmlNodeType.Element)
            {
                Add(reader.ReadElementContentAsString());
                //reader.ReadEndElement();
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            foreach (var item in this)
            {
                writer.WriteElementString("item", item.ToString());
            }
        }
    }
}