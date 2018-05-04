using System;
using System.Xml;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace XMLParser
{
    class Program
    {
        static void Main(string[] args)
        {
            //string _byteOrderMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
            //if (xml.StartsWith(_byteOrderMarkUtf8))
            //{
            //    xml = xml.Remove(0, _byteOrderMarkUtf8.Length);
            //}
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(args[0]);
            var elements = xmlDocument.LastChild.ChildNodes;
            var nodeNames = new List<(string, List<string>)> ();
            foreach (XmlNode item in elements)
            {
                nodeNames.Add((item.Name, GetNodeNames(item.ChildNodes)));
            }
            var orderedNodes = nodeNames.OrderBy(x => x.Item1);
            var distinctNodes = orderedNodes.Distinct(new TupleComparer());
            File.WriteAllLines(".\\test.txt", ConvertToStringArray(distinctNodes).Append($"Count of Different Nodes: {distinctNodes.Count()}"));
        }

        static string[] ConvertToStringArray(IEnumerable<(string, List<string>)> ps) => ps.Select(x => x.Item1 + "\n" + String.Join("\n -", x.Item2.ToArray())).ToArray();

        static List<string> GetNodeNames(XmlNodeList nodeList)
        {
            var returnList = new List<string>();
            foreach (XmlNode item in nodeList)
            {
                returnList.Add(item.Name);
            }
            return returnList;
        }


    }

    public class TupleComparer : IEqualityComparer<(string, List<string>)>
    {
        public bool Equals((string, List<string>) x, (string, List<string>) y)
        {
            return x.Item1 == y.Item1&& x.Item2.Count == y.Item2.Count;
        }

        public int GetHashCode((string, List<string>) obj)
        {
            return obj.Item1.GetHashCode();
        }
    }
}
