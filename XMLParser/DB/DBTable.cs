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

        public override string ToString() => $"Create table {Name} ({String.Join(",", DBFields)}{KeyStatements()})";

        private string KeyStatements()
        {
            var primaryKey = PrimaryKeyStatement();
            var foreignKey = ForeignKeyStatement();
            return foreignKey.Length != 0 ? $"{primaryKey} ,{foreignKey}" : primaryKey ;
        }

        private string PrimaryKeyStatement() => PrimaryKey != null ? $", Primary key({PrimaryKey.Name})" : string.Empty;

        private string ForeignKeyStatement()
        {
            var foreignKeyDefinitions = DBFields.Where(x => x.DBFieldKeyType == DBFieldKeyType.ForeignKey).Select(x => $"FOREIGN KEY ({x.Name}) REFERENCES {x.ForeignKeyReferences[0].Table.Name}({x.ForeignKeyReferences[0].Field.Name})");
            return String.Join(",", foreignKeyDefinitions);
        }

    }
}
