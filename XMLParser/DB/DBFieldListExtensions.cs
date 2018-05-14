using System;
using System.Collections.Generic;
using System.Linq;

namespace XMLParser.DB
{
    static class DBFieldListExtensions
    {
        public static bool AddReference(this List<DBField> dBFields, DBTable table, DBField field)
        {
            return dBFields.Select(x => x.AddReference(table, new List<DBField>() { field })).Aggregate((x, y) => x & y);
        }
    }
}
