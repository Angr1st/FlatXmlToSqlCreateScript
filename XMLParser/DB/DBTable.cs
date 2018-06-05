using System;
using System.Collections.Generic;
using System.Linq;

namespace XMLParser.DB
{
    class DBTable
    {
        public string Name { get; }
        public List<DBField> DBFields { get; private set; }
        public (List<DBField> primaryKeyFields, DBFieldKeyType pkType) PrimaryKey { get { return DBFields.Count(fields => fields.DBFieldKeyType == DBFieldKeyType.PrimaryKey) == 1 ? (DBFields.FindAll(x => x.DBFieldKeyType == DBFieldKeyType.PrimaryKey), DBFieldKeyType.PrimaryKey) : DBFields.Count(fields => fields.DBFieldKeyType.HasFlag( DBFieldKeyType.ClusteredPrimaryKey)) > 1 ? (DBFields.FindAll(x => x.DBFieldKeyType.HasFlag(DBFieldKeyType.ClusteredPrimaryKey)), DBFieldKeyType.ClusteredPrimaryKey) : default; } }

        public DBTable(string name, List<DBField> fields)
        {
            Name = name;
            DBFields = fields;
        }

        public override string ToString() => PrintCreateStatement();

        public string PrintCreateStatement(bool WithForeignKeys = true) => $"Create table {Name} ({String.Join(",", DBFields)}{KeyStatements(WithForeignKeys)})";

        public string PrintDBStructure() => $"{Name};{String.Join("", DBFields.Select(field => field.PrintStructure())).TrimEnd(';')}";

        private string KeyStatements(bool WithForeignKeys = true)
        {
            var primaryKey = PrimaryKeyStatement();
            var foreignKey = ForeignKeyStatement();
            return foreignKey.Length != 0 && WithForeignKeys ? $"{primaryKey} ,{foreignKey}" : primaryKey ;
        }

        public bool AddDefaultFieldToTable()
        {
            var defaultField = new DBField("pdfimportid", (DBFieldType.integer, 0), DBFieldKeyType.ClusteredPrimaryKey);
            DBFields.Add(defaultField);
            TurnAllPrimaryKeyEntrysToClusteredPrimaryKey();
            return true;
        }

        private void TurnAllPrimaryKeyEntrysToClusteredPrimaryKey()
        {
            if (PrimaryKey.pkType.HasFlag(DBFieldKeyType.PrimaryKey))
            {
                PrimaryKey.primaryKeyFields.Select(primaryKeys => { primaryKeys.MakeClusteredPrimaryKey(); return true; }).ToList();
            }
        }

        private string PrimaryKeyStatement()
        {
            return PrimaryKey != default(ValueTuple<List<DBField>,DBFieldKeyType>) ? $", Primary key({String.Join(",", PrimaryKey.primaryKeyFields.Select(fields => fields.Name))})" : string.Empty;
        }
        private string ForeignKeyStatement()
        {
            var foreignKeyDefinitions = DBFields.Where(field => field.DBFieldKeyType.HasFlag(DBFieldKeyType.ForeignKey) && field.ReferencesPrimaryKey).Select(field => $"FOREIGN KEY ({field.Name}) REFERENCES {field.ReferencedPrimaryKey.Table.Name}({field.ReferencedPrimaryKey.Field.Name})");
            return String.Join(",", foreignKeyDefinitions);
        }

    }
}
