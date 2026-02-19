using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ORM.Mapping
{
    public class EntityMetadata
    {
        public Type EntityType { get; set; } = null!;
        public string TableName { get; set; } = null!;
        public List<PropertyMetadata> Properties { get; set; } = new();
        public PropertyMetadata? PrimaryKey { get; set; }
        public List<PropertyMetadata> ForeignKeys { get; set; } = new();
        public List<PropertyMetadata> NavigationProperties { get; set; } = new();
    }

    public class PropertyMetadata
    {
        public string PropertyName { get; set; } = null!;
        public Type PropertyType { get; set; } = null!;
        public string ColumnName { get; set; } = null!;
        public string DatabaseType { get; set; } = null!;

        public bool IsPrimaryKey { get; set; }
        public bool AutoIncrement { get; set; }
        public bool IsNullable { get; set; }
        public bool IsUnique { get; set; }
        public bool IsNotNull { get; set; }
        public object? DefaultValue { get; set; }

        public bool IsForeignKey { get; set; }
        public string? ReferencedTable { get; set; }
        public string? ReferencedColumn { get; set; }
        public string? OnDelete { get; set; }
        public string? OnUpdate { get; set; }

        public bool IsNavigationProperty { get; set; }
        public string? NavigationType { get; set; }
        public string? ForeignKeyProperty { get; set; }

        public int Length { get; set; } = -1;
        public int Precision { get; set; } = -1;
        public int Scale { get; set; } = -1;
    }
}
