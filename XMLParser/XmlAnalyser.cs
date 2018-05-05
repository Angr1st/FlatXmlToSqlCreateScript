using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Linq;
using System.IO;

namespace XMLParser
{
    class XmlAnalyser
    {
        public void Execute(string[] args)
        {
            XmlDocument xmlDocument = LoadXML(args[0]);

            IEnumerable<(string, List<string>)> distinctNodes = ExtractUniqueNodeNames(xmlDocument);
            File.WriteAllLines(".\\test.txt", ConvertToStringArray(distinctNodes).Append($"Count of Different Nodes: {distinctNodes.Count()}"));
        }

        private IEnumerable<(string, List<string>)> ExtractUniqueNodeNames(XmlDocument xmlDocument)
        {
            var elements = xmlDocument.LastChild.ChildNodes;
            var nodeNames = new List<(string, List<string>)>();
            foreach (XmlNode item in elements)
            {
                nodeNames.Add((item.Name, GetNodeNames(item.ChildNodes)));
            }
            var orderedNodes = nodeNames.OrderBy(x => x.Item1);
            var distinctNodes = orderedNodes.Distinct(new TupleComparer());
            return distinctNodes;
        }

        private static XmlDocument LoadXML(string fileName)
        {
            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.Load(fileName);
                return xmlDoc;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private string[] ConvertToStringArray(IEnumerable<(string, List<string>)> ps) => ps.Select(x => x.Item1 + "\n -" + String.Join("\n -", x.Item2.ToArray())).ToArray();

        private List<string> GetNodeNames(XmlNodeList nodeList)
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
            return x.Item1 == y.Item1 && x.Item2.Count == y.Item2.Count && CompareListByItems(x.Item2, y.Item2);
        }

        private bool CompareListByItems(List<string> x, List<string> y)
        {
            var xOrdered = x.OrderBy(z => z);
            var yOrdered = y.OrderBy(z => z);
            return xOrdered.Zip(yOrdered, (t, r) => t == r ).Aggregate((w,e) => w&&e);
        }

        public int GetHashCode((string, List<string>) obj)
        {
            return obj.Item1.GetHashCode();
        }
    }
}

