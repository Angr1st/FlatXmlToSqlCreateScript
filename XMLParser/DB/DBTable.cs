using System;
using System.Collections.Generic;
using System.Linq;

namespace XMLParser.DB
{
    class DBTable
    {
        public string Name { get; }
        public List<DBField> DBFields { get; }
        public DBField PrimaryKey { get { return DBFields.Find(x => x.DBFieldKeyType == DBFieldKeyType.PrimaryKey); } }

        public DBTable(string name, List<DBField> fields)
        {
            Name = name;
            DBFields = fields;
        }

        public override string ToString()
        {
            return $"Create table {Name} ({String.Join(",", DBFields)}{PrimaryKeyStatement()})";
        }

        private string PrimaryKeyStatement()
        {
            return PrimaryKey != null ? $", Primary key({PrimaryKey.Name})" : string.Empty;
        }
    }
}
