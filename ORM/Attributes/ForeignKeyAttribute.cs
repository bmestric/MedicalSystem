using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ORM.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ForeignKeyAttribute : Attribute
    {
        public string ReferencedTable { get; }
        public string ReferencedColumn { get; set; } = "id";

        public bool Nullable { get; set; } = false;
        public string? OnDelete { get; set; } = "NO ACTION";
        public string? OnUpdate { get; set; } = "NO ACTION";

        public ForeignKeyAttribute(string referencedTable)
        {
            ReferencedTable = referencedTable;
        }
    }
}
