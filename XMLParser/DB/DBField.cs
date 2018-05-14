using System;
using System.Collections.Generic;
using System.Linq;

namespace XMLParser.DB
{
    [Flags]
    public enum DBFieldKeyType
    {
        PrimaryKey = 1,
        ForeignKey = 2,
        Value = 4
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
        public List<(DBTable Table, DBField Field, DBFieldKeyType ReferenceDirection)> ForeignKeyReferences { get; private set; }

        public DBField(string name, DBFieldType dBFieldType, DBFieldKeyType dBFieldKeyType)
        {
            Name = name;
            DBFieldKeyType = dBFieldKeyType;
            DBFieldType = dBFieldType;
        }

        public void MakePrimaryKey() => DBFieldKeyType = DBFieldKeyType | DBFieldKeyType.PrimaryKey;

        public bool AddReference(DBTable table, List<DBField> field)
        {
            return field.Select(x => AddReferenceInternal(table, x)).Aggregate( (x,y) => x & y);
        }

        private bool AddReferenceInternal(DBTable table, DBField field)
        {
            if (field.DBFieldKeyType == DBFieldKeyType.PrimaryKey)
            {
                if (ForeignKeyReferences.Exists(x => x.ReferenceDirection == DBFieldKeyType.ForeignKey))
                    throw new Exception("We have references alread!?");
                ForeignKeyReferences = new List<(DBTable Table, DBField Field, DBFieldKeyType ReferenceDirection)>() { (table, field, DBFieldKeyType.ForeignKey) };
                return true;
            }
            else if (DBFieldKeyType.PrimaryKey == DBFieldKeyType && field.DBFieldKeyType == DBFieldKeyType.ForeignKey)
            {
                if (ForeignKeyReferences == null)
                {
                    ForeignKeyReferences = new List<(DBTable Table, DBField Field, DBFieldKeyType ReferenceDirection)>();
                }

                ForeignKeyReferences.Add((table, field, DBFieldKeyType.PrimaryKey));
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
