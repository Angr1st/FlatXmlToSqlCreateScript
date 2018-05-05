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
        public DBFieldKeyType DBFieldKeyType { get; private set; }
        public List<(DBTable Table, DBField Field)> ForeignKeyReferences { get; private set; }

        public DBField(string name, DBFieldType dBFieldType, DBFieldKeyType dBFieldKeyType)
        {
            Name = name;
            DBFieldKeyType = dBFieldKeyType;
            DBFieldType = dBFieldType;
        }

        public void MakePrimaryKey() => DBFieldKeyType = DBFieldKeyType.PrimaryKey;

        public bool AddReference(DBTable table, DBField field)
        {
            if (field.DBFieldKeyType == DBFieldKeyType.PrimaryKey)
            {
                DBFieldKeyType = DBFieldKeyType.ForeignKey;
                if (ForeignKeyReferences != null)
                    throw new Exception("We have references alread!?");
                ForeignKeyReferences = new List<(DBTable Table, DBField Field)>() { (table, field) };
                return true;
            }
            else if (DBFieldKeyType.PrimaryKey == DBFieldKeyType && field.DBFieldKeyType == DBFieldKeyType.ForeignKey)
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
                    dataType = "int";
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

        public override bool Equals(object obj)
        {
            if (obj is DBField)
            {
                var dbField = obj as DBField;
                if (dbField.Name == Name && dbField.DBFieldType == DBFieldType)
                {
                    return true;
                }
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ DBFieldKeyType.GetHashCode() ^ DBFieldType.GetHashCode();
        }
    }
}
