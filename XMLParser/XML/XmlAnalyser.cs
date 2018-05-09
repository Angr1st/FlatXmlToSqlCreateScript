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
            var manualyIdentifiedPrimaryKeys = LoadPrimaryKeyFile(args[1]);
            IEnumerable<DBTable> distinctNodes = ExtractUniqueNodeNames(xmlDocument, manualyIdentifiedPrimaryKeys);
            File.WriteAllLines(".\\test.txt", ConvertToStringArray(distinctNodes).Append($"Count of Different Nodes: {distinctNodes.Count()}"));
        }

        private Dictionary<string, string> LoadPrimaryKeyFile(string fileName)
        {
            Dictionary<string, string> returnDict = new Dictionary<string, string>();
           var lines = File.ReadAllLines(fileName);
            foreach (var line in lines)
            {
                var splittedLine = line.Split(';');
                returnDict.Add(splittedLine[0], splittedLine[1]);
            }
            return returnDict;
        }

        private IEnumerable<DBTable> ExtractUniqueNodeNames(XmlDocument xmlDocument, Dictionary<string, string> primaryKeys)
        {
            var elements = xmlDocument.LastChild.ChildNodes;
            var nodeNames = new List<DBTable>();
            foreach (XmlNode item in elements)
            {
                string primaryKeyName = string.Empty;
                if (primaryKeys.Keys.Contains(item.Name))
                {
                    primaryKeyName = primaryKeys[item.Name];
                }

                var existingNodes = nodeNames.Where(x => x.Name == item.Name);
                if (existingNodes.Count() != 0)
                {
                    //There is only ever one node with the same name already in the List
                    var existingNode = existingNodes.First();
                    var subNodeNameList = existingNode.DBFields;
                    subNodeNameList.AddRange(GetNodeNames(item.ChildNodes, item.Name, primaryKeyName));
                    subNodeNameList = subNodeNameList.Distinct().ToList();
                    DBTable newEntry = new DBTable(existingNode.Name, subNodeNameList);
                    nodeNames.Remove(existingNode);
                    nodeNames.Add(newEntry);
                }
                else
                {
                    nodeNames.Add(new DBTable(item.Name, GetNodeNames(item.ChildNodes, item.Name, primaryKeyName)));
                }
            }
            var orderedNodes = nodeNames.OrderBy(x => x.Name);
            var distinctNodes = orderedNodes.Distinct();
            EstablishForeignKeyRelations(distinctNodes);
            return distinctNodes;
        }

        private bool EstablishForeignKeyRelations(IEnumerable<DBTable> tables)
        {
            return tables.Select(x => (x.DBFields.First(), x))
                .SelectMany(x => tables.SelectMany(y => y.DBFields.Where(z => z.Name == x.Item1.Name && x.x.Name != y.Name).Select(z => (x, y, z))))
                .Select(x =>
                {
                    var resultForeignKey = x.z.AddReference(x.x.x, x.x.Item1);
                    var resultPrimaryKey = x.x.Item1.AddReference(x.y, x.z);
                    return resultForeignKey && resultPrimaryKey;
                })
                .Aggregate((x, y) => x && y);
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

        private string[] ConvertToStringArray(IEnumerable<DBTable> ps) => ps.Select(x => x.ToString() + ";").ToArray();

        private List<DBField> GetNodeNames(XmlNodeList nodeList, string nodeName, string primaryKeyName)
        {
            var returnList = new List<DBField>();
            foreach (XmlNode item in nodeList)
            {
                returnList.Add(new DBField(item.Name, AnalyseValue(item.InnerText), DBFieldKeyType.Value));
            }
            if (primaryKeyName != string.Empty)
            {
                var primaryKeyCandidateList = returnList.Where(x => x.Name == primaryKeyName);
                if (primaryKeyCandidateList.Count() == 1)
                {
                    var primaryKeyCandidate = primaryKeyCandidateList.First();
                    if (primaryKeyCandidate.DBFieldKeyType != DBFieldKeyType.PrimaryKey)
                    {
                        primaryKeyCandidate.MakePrimaryKey();
                    }
                }
            }
            else
            {
                var pkCandidates = returnList.Where(pkCandidate => pkCandidate.Name.StartsWith(nodeName) && pkCandidate.Name.EndsWith("ID"));
                if (pkCandidates.Count() == 1)
                {
                    pkCandidates.First().MakePrimaryKey();
                }
            }
            return returnList;
        }

        private DBFieldType AnalyseValue(string value)
        {
            if (value.Length < 25 && value.Contains("."))
            {
                var (parseSuccess, parsedType) = TryParse(value, DBFieldType.@double);
                return parseSuccess ? parsedType : DBFieldType.varchar;
            }
            else if (value.Length < 25)
            {
                var (parseSuccess, parsedType) = TryParse(value, DBFieldType.integer);
                return parseSuccess ? parsedType : DBFieldType.varchar;
            }
            else if (value.Length == 25)
            {
                var (parseSuccess, parsedType) = TryParse(value, DBFieldType.dateTime);
                return parseSuccess ? parsedType : DBFieldType.varchar;
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
}

