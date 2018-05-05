using System;
using System.Collections.Generic;
using System.Text;

namespace XMLParser.DB
{
    public enum DBFieldKeyType
    {
        PrimaryKey,
        ForeignKey,
        Value
    }

    public enum DBFieldType
    {
        unkown,
        varchar,
        integer,
        @double,
        dateTime
    }

    class DBField
    {
        public string Name { get; }
        public DBFieldType DBFieldType { get; }
        public DBFieldKeyType DBFieldKeyType { get; }
        public List<(DBTable Table, DBField Field)> ForeignKeyReferences { get; private set; }

        public DBField(string name, DBFieldType dBFieldType, DBFieldKeyType dBFieldKeyType)
        {
            Name = name;
            DBFieldKeyType = dBFieldKeyType;
            DBFieldType = dBFieldType;
        }

        public bool AddReference(DBTable table, DBField field)
        {
            if (DBFieldKeyType.PrimaryKey == DBFieldKeyType && field.DBFieldKeyType == DBFieldKeyType.ForeignKey)
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

        public override string ToString()
        {
            string dataType = string.Empty;
            switch (DBFieldType)
            {
                case DBFieldType.varchar:
                    dataType = "varchar(200)";
                    break;
                case DBFieldType.integer:
                    dataType = "integer";
                    break;
                case DBFieldType.@double:
                    dataType = "double";
                    break;
                case DBFieldType.dateTime:
                    dataType = "DateTime";
                    break;
                default:
                    break;
            }

            return $"{Name} {dataType}";
        }
    }
}
