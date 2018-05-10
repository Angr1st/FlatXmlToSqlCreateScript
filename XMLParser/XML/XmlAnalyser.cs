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
            if (args.Length == 0 )
            {
                throw new ArgumentException("You need to pass in the path to the xml file that should be analysed.");
            }
            if (args.Length > 2)
            {
                throw new ArgumentException("You passed in to many arguments. Only two arguments are allowed. The first one to specify the location of the xml and the second optional argument for the manualy specified primary keys.")
            }

            XmlDocument xmlDocument = LoadXML(args[0]);
            Dictionary<string, string> manualyIdentifiedPrimaryKeys = new Dictionary<string, string>();
            if (args.Length == 2)
            {
                manualyIdentifiedPrimaryKeys = LoadPrimaryKeyFile(args[1]);
            }

            IEnumerable<DBTable> distinctNodes = ExtractUniqueNodeNames(xmlDocument, manualyIdentifiedPrimaryKeys);
            File.WriteAllLines(".\\createTables.txt", ConvertToStringArray(distinctNodes).Append($"Count of Different Nodes: {distinctNodes.Count()}"));
        }

        private Dictionary<string, string> LoadPrimaryKeyFile(string fileName)
        {
            Dictionary<string, string> returnDict = new Dictionary<string, string>();
            try
            {
                var lines = File.ReadAllLines(fileName);
                foreach (var line in lines)
                {
                    if (line.Length != 0)
                    {
                        var splittedLine = line.Split(';');
                        returnDict.Add(splittedLine[0], splittedLine[1]);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error when loading the primary key file!", e);
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
                    var distinctWithoutUnkowns = subNodeNameList.Distinct().Where(x => x.DBFieldType != DBFieldType.unkown);
                    var allDoubleDifferentTyp = distinctWithoutUnkowns.SelectMany(x => distinctWithoutUnkowns.Where(y => x.Name == y.Name && x.DBFieldType != y.DBFieldType)).Where(x => x.DBFieldType != DBFieldType.@double).Distinct();
                    subNodeNameList = distinctWithoutUnkowns.Except(allDoubleDifferentTyp).ToList();
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

            return distinctNodes.OrderBy(x => GetNumberForOrdering(x.PrimaryKey, x.DBFields.Where(y => y.DBFieldKeyType == DBFieldKeyType.ForeignKey)));
        }

        private int GetNumberForOrdering(DBField primaryKey, IEnumerable<DBField> foreignKeys)
        {
            int orderNumber = 1;
            if (primaryKey == null)
            {
                orderNumber += 999;
            }
            var numberOfForeignKeys = foreignKeys.Count();
            orderNumber += numberOfForeignKeys;

            int highestDependingOrderingNumber = 0;
            if (numberOfForeignKeys != 0)
            {
                highestDependingOrderingNumber = foreignKeys.Select(x => { var (Table, Field) = x.ForeignKeyReferences.First(); return GetNumberForOrdering(Table.PrimaryKey, Table.DBFields.Where(y => y.DBFieldKeyType == DBFieldKeyType.ForeignKey)); }).Aggregate((x, y) => x >= y ? x : y);
            }
            //Check if all the tables this table depends on have a lower orderNumber


            return highestDependingOrderingNumber >= orderNumber ? highestDependingOrderingNumber + 1 : orderNumber;
        }

        private bool EstablishForeignKeyRelations(IEnumerable<DBTable> tables)
        {
            return tables.Select(x => (x.PrimaryKey, x)).Where(x => x.PrimaryKey != null)
                .SelectMany(x => tables.SelectMany(y => y.DBFields.Where(z => z.Name == x.PrimaryKey.Name && x.x.Name != y.Name).Select(z => (x, y, z))))
                .Select(x =>
                {
                    var resultForeignKey = x.z.AddReference(x.x.x, x.x.PrimaryKey);
                    var resultPrimaryKey = x.x.PrimaryKey.AddReference(x.y, x.z);
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
            catch (Exception e)
            {
                throw new Exception("Error while loading the XML.", e);
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
                else if (pkCandidates.Count(x => x.Name == $"{nodeName}ID") == 1)
                {
                    pkCandidates.First(x => x.Name == $"{nodeName}ID").MakePrimaryKey();
                }
            }
            return returnList;
        }

        private DBFieldType AnalyseValue(string value)
        {
            if (value.Length == 0)
            {
                return DBFieldType.unkown;
            }
            else if (value.Length < 25 && value.Contains("."))
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

