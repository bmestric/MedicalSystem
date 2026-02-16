using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ORM.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class PrimaryKeyAttribute : Attribute
    {
        public bool AutoIncrement { get; set; } = false;

        public PrimaryKeyAttribute()
        {
        }

        public PrimaryKeyAttribute(bool autoIncrement)
        {
            AutoIncrement = autoIncrement;
        }
    }
}
