using ORM.Attributes;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ORM.Mapping
{
    public class EntityMapper
    {
        private static readonly ConcurrentDictionary<Type, EntityMetadata> _metadata = new();

        public static EntityMetadata GetMetadata(Type entityType)
        {
            if (!_metadata.TryGetValue(entityType, out var metadata))
            {
                metadata = BuildMetadata(entityType);
                _metadata[entityType] = metadata; // cache it
            }
            return metadata;
        }

        private static EntityMetadata BuildMetadata(Type entityType)
        {
            var metadata = new EntityMetadata
            {
                EntityType = entityType,
                TableName = GetTableName(entityType)
            };

            var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var property in properties)
            {
                if (IsCollectionProperty(property))
                {
                    var navProperty = BuildNavigationPropertyMetadata(property);
                    metadata.NavigationProperties.Add(navProperty);
                    continue;
                }

                var navAttr = property.GetCustomAttribute<NavigationPropertyAttribute>();
                if (navAttr != null)
                {
                    var navProperty = BuildNavigationPropertyMetadata(property);
                    metadata.NavigationProperties.Add(navProperty);
                    continue;
                }

                var propertyMetadata = BuildPropertyMetadata(property);
                metadata.Properties.Add(propertyMetadata);

                if (propertyMetadata.IsPrimaryKey)
                {
                    metadata.PrimaryKey = propertyMetadata;
                }

                if (propertyMetadata.IsForeignKey)
                {
                    metadata.ForeignKeys.Add(propertyMetadata);
                }
            }

            return metadata;
        }

        private static PropertyMetadata BuildPropertyMetadata(PropertyInfo property)
        {
            var columnAttr = property.GetCustomAttribute<ColumnAttribute>();
            var pkAttr = property.GetCustomAttribute<PrimaryKeyAttribute>();
            var fkAttr = property.GetCustomAttribute<ForeignKeyAttribute>();
            var uniqueAttr = property.GetCustomAttribute<UniqueAttribute>();
            var notNullAttr = property.GetCustomAttribute<NotNullAttribute>();
            var defaultAttr = property.GetCustomAttribute<DefaultValueAttribute>();

            var length = columnAttr?.Length ?? -1;
            var precision = columnAttr?.Precision ?? -1;
            var scale = columnAttr?.Scale ?? -1;

            var metadata = new PropertyMetadata
            {
                PropertyName = property.Name,
                PropertyType = property.PropertyType,
                ColumnName = columnAttr?.Name ?? property.Name,
                DatabaseType = columnAttr?.TypeName ?? TypeMapper.GetDatabaseType(property.PropertyType, length, precision, scale),
                IsPrimaryKey = pkAttr != null,
                AutoIncrement = pkAttr?.AutoIncrement ?? false,
                IsNullable = TypeMapper.IsNullable(property.PropertyType),
                IsUnique = uniqueAttr != null,
                IsNotNull = notNullAttr != null,
                DefaultValue = defaultAttr?.Value,
                IsForeignKey = fkAttr != null,
                ReferencedTable = fkAttr?.ReferencedTable,
                ReferencedColumn = fkAttr?.ReferencedColumn,
                OnDelete = fkAttr?.OnDelete,
                OnUpdate = fkAttr?.OnUpdate,
                Length = length,
                Precision = precision,
                Scale = scale
            };

            return metadata;
        }


        private static PropertyMetadata BuildNavigationPropertyMetadata(PropertyInfo property)
        {
            var navAttr = property.GetCustomAttribute<NavigationPropertyAttribute>();

            return new PropertyMetadata
            {
                PropertyName = property.Name,
                PropertyType = property.PropertyType,
                ColumnName = property.Name,
                DatabaseType = "NAVIGATION",
                IsNavigationProperty = true,
                NavigationType = navAttr?.Type.ToString() ?? "OneToMany",
                ForeignKeyProperty = navAttr?.ForeignKey
            };
        }

        private static bool IsCollectionProperty(PropertyInfo property)
        {
            var type = property.PropertyType;

            if (type == typeof(string))
                return false;

            return type.IsGenericType &&
                   (type.GetGenericTypeDefinition() == typeof(ICollection<>) ||
                    type.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
                    type.GetGenericTypeDefinition() == typeof(List<>) ||
                    type.GetGenericTypeDefinition() == typeof(IList<>));
        }
        

        private static string GetTableName(Type entityType)
        {
            var tableAttr = entityType.GetCustomAttribute<TableAttribute>();
            return tableAttr?.Name ?? entityType.Name;
        }
    }
}
