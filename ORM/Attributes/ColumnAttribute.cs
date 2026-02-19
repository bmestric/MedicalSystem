using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ORM.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnAttribute : Attribute
    {
        public string? Name { get; set; }
        public string? TypeName { get; set; }
        public int Length { get; set; } = -1;
        public int Precision { get; set; } = -1;
        public int Scale { get; set; } = -1;

        public ColumnAttribute()
        {

        }

        public ColumnAttribute(string name)
        {
            Name = name;
        }
    }
}
