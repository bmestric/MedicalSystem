using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ORM.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class NavigationPropertyAttribute : Attribute
    {
        public string? ForeignKey { get; set; }
        public NavigationType Type { get; set; }

        public NavigationPropertyAttribute(NavigationType type)
        {
            Type = type;
        }

    }

    public enum NavigationType
    {
        OneToOne,
        OneToMany,
        ManyToOne,
        ManyToMany
    }
}
