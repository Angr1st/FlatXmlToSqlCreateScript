using System;
using System.Collections.Generic;
using System.Linq;

namespace XMLParser.DB
{
    static class DBFieldListExtensions
    {
        public static bool AddReference(this List<DBField> dBFields, DBTable table, DBField field, DBFieldKeyType direction)
        {
            return dBFields.Select(x => x.AddReference(table, new List<DBField>() { field }, direction)).Aggregate((x, y) => x & y);
        }
    }
}
