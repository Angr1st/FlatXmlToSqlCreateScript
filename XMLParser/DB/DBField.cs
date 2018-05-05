using System;
using System.Collections.Generic;
using System.Text;

namespace XMLParser.DB
{
    public enum DBFieldType
    {
        PrimaryKey,
        ForeignKey,
        Value
    }

    class DBField
    {
        public string Name { get; }
        public DBFieldType DBFieldType { get; }
        public List<(DBTable Table,DBField Field)> ForeignKeyReferences { get; private set; }

        public DBField(string name, DBFieldType dBFieldType)
        {
            Name = name;
            DBFieldType = dBFieldType;
        }

        public bool AddReference(DBTable table, DBField field)
        {
            if (DBFieldType.PrimaryKey == DBFieldType && field.DBFieldType == DBFieldType.ForeignKey)
            {
                if (ForeignKeyReferences == null)
                {
                    ForeignKeyReferences = new List<(DBTable Table, DBField Field)>();
                }

                ForeignKeyReferences.Add((table, field));
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}
