using System;
using System.Collections.Generic;
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
                throw new ArgumentException("You passed in to many arguments. Only two arguments are allowed. The first one to specify the location of the xml and the second optional argument for the manualy specified primary keys.");
            }

            XmlDocument xmlDocument = LoadXML(args[0]);
            Dictionary<string, List<string>> manualyIdentifiedPrimaryKeys = new Dictionary<string, List<string>>();
            if (args.Length == 2)
            {
                manualyIdentifiedPrimaryKeys = LoadPrimaryKeyFile(args[1]);
            }

            IEnumerable<DBTable> distinctNodes = ExtractUniqueNodeNames(xmlDocument, manualyIdentifiedPrimaryKeys);
            File.WriteAllLines(".\\createTables.txt", ConvertToStringArray(distinctNodes).Append($"Count of Different Nodes: {distinctNodes.Count()}"));
            File.WriteAllLines(".\\dbstructure.txt", ConvertToStringArray(distinctNodes, false));
        }

        private Dictionary<string,List<string>> LoadPrimaryKeyFile(string fileName)
        {
            Dictionary<string, List<string>> returnDict = new Dictionary<string, List<string>>();
            try
            {
                var lines = File.ReadAllLines(fileName);
                foreach (var line in lines)
                {
                    if (line.Length != 0)
                    {
                        var splittedLine = line.Split(';');
                        var stringList = new List<string>();
                        stringList.AddRange(splittedLine.Skip(1).TakeWhile(x => !string.IsNullOrEmpty(x)));
                        returnDict.Add(splittedLine[0],stringList);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error when loading the primary key file!", e);
            }
            return returnDict;
        }

        private IEnumerable<DBTable> ExtractUniqueNodeNames(XmlDocument xmlDocument, Dictionary<string, List<string>> primaryKeys)
        {
            var elements = xmlDocument.LastChild.ChildNodes;
            var nodeNames = new List<DBTable>();
            foreach (XmlNode item in elements)
            {
                List<string> primaryKeyName = new List<string>();
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
                    var distinctWithoutUnkowns = subNodeNameList.OrderByDescending(field => field.Length).Distinct().Where(x => x.DBFieldType != DBFieldType.unkown);
                    var allDoubleDifferentTyp = distinctWithoutUnkowns.SelectMany(x => distinctWithoutUnkowns.Where(y => x.Name == y.Name && x.DBFieldType != y.DBFieldType)).Where(x => x.DBFieldType != DBFieldType.@double && x.DBFieldType != DBFieldType.varchar).Distinct();
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

            return distinctNodes.OrderBy(table => GetNumberForOrdering(table.PrimaryKey.primaryKeyFields, table.DBFields.Where(y => y.DBFieldKeyType.HasFlag(DBFieldKeyType.ForeignKey))));
        }

        private int GetNumberForOrdering(List<DBField> primaryKey, IEnumerable<DBField> foreignKeys)
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
                highestDependingOrderingNumber = foreignKeys.Select(x => { var (Table, Field, ReferenceDirection) = x.ForeignKeyReferences.Where(z => z.ReferenceDirection == DBFieldKeyType.PrimaryKey).First(); return GetNumberForOrdering(Table.PrimaryKey.primaryKeyFields, Table.DBFields.Where(y => y.DBFieldKeyType.HasFlag( DBFieldKeyType.ForeignKey))); }).Aggregate((x, y) => x >= y ? x : y);
            }
            //Check if all the tables this table depends on have a lower orderNumber


            return highestDependingOrderingNumber >= orderNumber ? highestDependingOrderingNumber + 1 : orderNumber;
        }

        private bool EstablishForeignKeyRelations(IEnumerable<DBTable> tables)
        {
            var primaryKeys = tables.Select(table => (table.PrimaryKey, table))
                .Where(touple => touple.PrimaryKey.pkType == DBFieldKeyType.PrimaryKey)
                .SelectMany( primaryKeyAndTable => tables
                .SelectMany(otherTable => otherTable.DBFields
                .Where(otherTableFields => primaryKeyAndTable.PrimaryKey.primaryKeyFields.Contains(otherTableFields) && primaryKeyAndTable.table.Name != otherTable.Name)
                .Select(foreignField => (primaryKeyAndTable, otherTable, foreignField))))
                .Select(pktbfk => 
                {
                    var resultForeignKey = pktbfk.foreignField.AddReference(pktbfk.primaryKeyAndTable.table, pktbfk.primaryKeyAndTable.PrimaryKey.primaryKeyFields, DBFieldKeyType.PrimaryKey);
                    if (!resultForeignKey)
                    {
                        throw new Exception("Something went wrong with the foreignkey thing!");
                    }
                    var resultPrimaryKey = pktbfk.primaryKeyAndTable.PrimaryKey.primaryKeyFields.AddReference(pktbfk.otherTable, pktbfk.foreignField, DBFieldKeyType.ForeignKey);
                    if (!resultPrimaryKey)
                    {
                        throw new Exception("Something went wrong with the primary key!");
                    }
                    return resultForeignKey && resultPrimaryKey;
                }).Aggregate((x, y) => x && y);
            var clusteredPrimaryKeys = tables.Select(table => (table.PrimaryKey, table))
                .Where(touple => touple.PrimaryKey.pkType == DBFieldKeyType.ClusteredPrimaryKey)
                .ToList();
            return primaryKeys;
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

        private string[] ConvertToStringArray(IEnumerable<DBTable> ps, bool toString = true) =>ps.Select(x =>$"{(toString ? x.ToString(): x.PrintDBStructure())};").ToArray();

        private List<DBField> GetNodeNames(XmlNodeList nodeList, string nodeName, List<string> primaryKeyNameList)
        {
            var returnList = new List<DBField>();
            foreach (XmlNode item in nodeList)
            {
                returnList.Add(new DBField(item.Name, AnalyseValue(item.InnerText), DBFieldKeyType.Value));
            }
            if (primaryKeyNameList.Count != 0)
            {
                var primaryKeyCandidateList = returnList.Where(x => primaryKeyNameList.Contains(x.Name));
                if ( primaryKeyCandidateList.Count() == primaryKeyNameList.Count && primaryKeyNameList.Count != 1)
                {
                    foreach (var item in primaryKeyCandidateList)
                    {//Check if it is here where GPGPGr is not set properly; If this works look at where the fk is set.
                        item.MakeClusteredPrimaryKey();
                    }
                }
                else if (primaryKeyCandidateList.Count() == 1 )
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

        private (DBFieldType fieldType,int length) AnalyseValue(string value)
        {
            if (value.Length == 0)
            {
                return (DBFieldType.unkown,0);
            }
            else if (value.Length == 25 || value.Length == 29)
            {
                var (parseSuccess, parsedType) = TryParse(value, DBFieldType.dateTime);
                return parseSuccess ? (parsedType,value.Length) : (DBFieldType.varchar,value.Length);
            }
            else if (value.Length < 25 && value.Contains("."))
            {
                var (parseSuccess, parsedType) = TryParse(value, DBFieldType.@double);
                return parseSuccess ? (parsedType,0) : (DBFieldType.varchar, value.Length);
            }
            else if (value.Length < 25)
            {
                var (parseSuccess, parsedType) = TryParse(value, DBFieldType.integer);
                return parseSuccess ? (parsedType, 0) : (DBFieldType.varchar, value.Length);
            }
            else
            {
                return (DBFieldType.varchar, value.Length);
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

