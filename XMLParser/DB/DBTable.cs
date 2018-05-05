using System;
using System.Collections.Generic;
using System.Text;

namespace XMLParser.DB
{
    class DBTable
    {
        public string Name { get; }
        public List<DBField> DBFields { get; }

        public DBTable(string name, List<DBField> fields)
        {
            Name = name;
            DBFields = fields;
        }

        public override string ToString()
        {
            return $"Create table {Name} ({String.Join(",", DBFields)})";
        }
    }
}
