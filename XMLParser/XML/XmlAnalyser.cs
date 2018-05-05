using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Linq;
using System.IO;
using XMLParser.DB;

namespace XMLParser.XML
{
    class XmlAnalyser
    {
        public void Execute(string[] args)
        {
            XmlDocument xmlDocument = LoadXML(args[0]);

            IEnumerable<DBTable> distinctNodes = ExtractUniqueNodeNames(xmlDocument);
            File.WriteAllLines(".\\test.txt", ConvertToStringArray(distinctNodes).Append($"Count of Different Nodes: {distinctNodes.Count()}"));
        }

        private IEnumerable<DBTable> ExtractUniqueNodeNames(XmlDocument xmlDocument)
        {
            var elements = xmlDocument.LastChild.ChildNodes;
            var nodeNames = new List<DBTable>();
            foreach (XmlNode item in elements)
            {
                var existingNodes = nodeNames.Where(x => x.Name == item.Name);
                if (existingNodes.Count() != 0)
                {
                    //There is only ever one node with the same name already in the List
                    var existingNode = existingNodes.First();
                    var subNodeNameList = existingNode.DBFields;
                    subNodeNameList.AddRange(GetNodeNames(item.ChildNodes));
                    subNodeNameList = subNodeNameList.Distinct().ToList();
                    DBTable newEntry = new DBTable(existingNode.Name, subNodeNameList);
                    nodeNames.Remove(existingNode);
                    nodeNames.Add(newEntry);
                }
                else
                {
                    nodeNames.Add(new DBTable(item.Name, GetNodeNames(item.ChildNodes)));
                }
            }
            var orderedNodes = nodeNames.OrderBy(x => x.Name);
            var distinctNodes = orderedNodes.Distinct();
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

        private string[] ConvertToStringArray(IEnumerable<DBTable> ps) => ps.Select(x => x.Name + "\n -" + String.Join("\n -", x.DBFields)).ToArray();

        private List<DBField> GetNodeNames(XmlNodeList nodeList)
        {
            var returnList = new List<DBField>();
            foreach (XmlNode item in nodeList)
            {
                returnList.Add(new DBField(item.Name, AnalyseValue(item.Value), DBFieldKeyType.Value));
            }
            return returnList;
        }

        private DBFieldType AnalyseValue(string value)
        {
            if (value.Length < 25 && value.Contains("."))
            {
                var doubleResult = TryParse(value, DBFieldType.@double);
                return doubleResult.parseSuccess ? doubleResult.parsedType : DBFieldType.varchar;
            }
            else if (value.Length < 25)
            {
                var integerResult = TryParse(value, DBFieldType.integer);
                return integerResult.parseSuccess ? integerResult.parsedType : DBFieldType.varchar;
            }
            else if (value.Length == 25)
            {
                var dateResult = TryParse(value, DBFieldType.dateTime);
                return dateResult.parseSuccess ? dateResult.parsedType : DBFieldType.varchar;
            }
            else
            {
                return DBFieldType.varchar;
            }
        }

        private (bool parseSuccess, DBFieldType parsedType) TryParse(string value, DBFieldType dBFieldType)
        {
            switch (dBFieldType)
            {
                case DBFieldType.varchar:
                    return (true, DBFieldType.varchar);

                case DBFieldType.integer:
                    return TryParseInteger();

                case DBFieldType.@double:
                    return TryParseDouble();

                case DBFieldType.dateTime:
                    return TryParseDateTimeOffset();

                default:
                    return (false, DBFieldType.unkown);
            }

            (bool, DBFieldType) TryParseInteger()
            {
                var isInteger = int.TryParse(value, out int intValue);
                return isInteger ? (isInteger, DBFieldType.integer) : (isInteger, DBFieldType.unkown);
            }

            (bool, DBFieldType) TryParseDateTimeOffset()
            {
                var isDate = DateTimeOffset.TryParse(value, out DateTimeOffset dateTimeOffset);
                return isDate ? (isDate, DBFieldType.dateTime) : (isDate, DBFieldType.unkown);
            }

            (bool, DBFieldType) TryParseDouble()
            {
                var isDouble = Double.TryParse(value, out Double doubleValue);
                return isDouble ? (isDouble, DBFieldType.@double) : (isDouble, DBFieldType.unkown);
            }
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
            return xOrdered.Zip(yOrdered, (t, r) => t == r).Aggregate((w, e) => w && e);
        }

        public int GetHashCode((string, List<string>) obj)
        {
            return obj.Item1.GetHashCode();
        }
    }
}

