using System;
using System.Collections.Generic;
using System.Linq;

namespace XMLParser.DB
{
    [Flags]
    public enum DBFieldKeyType
    {
        Unkown = 0,
        PrimaryKey = 1,
        ForeignKey = 2,
        Value = 4,
        ClusteredPrimaryKey = 8
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
        public int Length { get; }
        public List<(DBTable Table, DBField Field, DBFieldKeyType ReferenceDirection)> ForeignKeyReferences { get; private set; }

        public bool ReferencesPrimaryKey { get { return ReferencedPrimaryKey != default; } }

        public (DBTable Table, DBField Field) ReferencedPrimaryKey
        {
            get
            {
                var (Table, Field, ReferenceDirection) = ForeignKeyReferences.Where(entry => entry.ReferenceDirection.HasFlag(DBFieldKeyType.PrimaryKey)).FirstOrDefault();
                return (Table, Field);
            }
        }

        public DBField(string name, (DBFieldType dBFieldType, int length) fieldType, DBFieldKeyType dBFieldKeyType)
        {
            Name = name;
            DBFieldKeyType = dBFieldKeyType;
            DBFieldType = fieldType.dBFieldType;
            Length = fieldType.length;
        }

        public void MakePrimaryKey()
        {
            if (DBFieldKeyType.HasFlag(DBFieldKeyType.Value))
            {
                DBFieldKeyType = DBFieldKeyType.PrimaryKey;
            }
            else
            {
                DBFieldKeyType = DBFieldKeyType | DBFieldKeyType.PrimaryKey;
            }
        }

        public void MakeClusteredPrimaryKey() => DBFieldKeyType = DBFieldKeyType.ClusteredPrimaryKey;

        private void MakeForeignKey()
        {
            if (DBFieldKeyType.HasFlag(DBFieldKeyType.Value))
            {
                DBFieldKeyType = DBFieldKeyType.ForeignKey;
            }
            else
            {
                DBFieldKeyType = DBFieldKeyType | DBFieldKeyType.ForeignKey;
            }
        }

        public bool AddReference(DBTable table, List<DBField> field, DBFieldKeyType direction)
        {
            return field.Select(x => AddReferenceInternal(table, x, direction)).Aggregate((x, y) => x & y);
        }

        private bool AddReferenceInternal(DBTable table, DBField field, DBFieldKeyType direction)
        {
            if (direction == DBFieldKeyType.PrimaryKey)//This means this field references a primary key and is a foreign key itself(there can only be one of these)
            {
                MakeForeignKey();
                if (ForeignKeyReferences != null && ForeignKeyReferences.Exists(x => x.ReferenceDirection == DBFieldKeyType.PrimaryKey))
                    throw new Exception("We have references alread!?");
                ForeignKeyReferences = new List<(DBTable Table, DBField Field, DBFieldKeyType ReferenceDirection)>() { (table, field, direction) };
                return true;
            }
            else if (direction == DBFieldKeyType.ForeignKey)
            {
                if (ForeignKeyReferences == null)
                {
                    ForeignKeyReferences = new List<(DBTable Table, DBField Field, DBFieldKeyType ReferenceDirection)>();
                }

                ForeignKeyReferences.Add((table, field, direction));
                return true;
            }
            else
            {
                return false;
            }
        }

        public string PrintStructure()
        {
            return $"{Name},{(DBFieldKeyType.HasFlag(DBFieldKeyType.PrimaryKey) ? "notnull" : "null")},{PrintDBFieldType()};";

            string PrintDBFieldType()
            {
                switch (DBFieldType)
                {
                    
                    case DBFieldType.varchar:
                        return $"{DBFieldType.ToString()},{Length}";

                    case DBFieldType.integer:
                        return "int";

                    case DBFieldType.@double:
                        return DBFieldType.ToString();

                    case DBFieldType.dateTime:
                        return "DateTime";

                    case DBFieldType.unkown:
                    default:
                        throw new Exception($"Unkown DBFieldType in {Name}");
                }
            }
        }

        public override string ToString()
        {
            string dataType = string.Empty;
            switch (DBFieldType)
            {
                case DBFieldType.varchar:
                    dataType = $"varchar({(Length < 10 ? Length : 200)})";
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
