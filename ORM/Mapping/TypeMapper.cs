using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ORM.Mapping
{
    public static class TypeMapper
    {
        private static readonly Dictionary<Type, string> _typeMap = new();

        static TypeMapper()
        {
            _typeMap[typeof(int)] = "INTEGER";
            _typeMap[typeof(int?)] = "INTEGER";
            _typeMap[typeof(long)] = "BIGINT";
            _typeMap[typeof(long?)] = "BIGINT";
            _typeMap[typeof(short)] = "SMALLINT";
            _typeMap[typeof(short?)] = "SMALLINT";
            _typeMap[typeof(byte)] = "SMALLINT";
            _typeMap[typeof(byte?)] = "SMALLINT";

            _typeMap[typeof(decimal)] = "DECIMAL";
            _typeMap[typeof(decimal?)] = "DECIMAL";
            _typeMap[typeof(double)] = "DOUBLE PRECISION";
            _typeMap[typeof(double?)] = "DOUBLE PRECISION";
            _typeMap[typeof(float)] = "REAL";
            _typeMap[typeof(float?)] = "REAL";

            _typeMap[typeof(string)] = "TEXT";
            _typeMap[typeof(char)] = "CHAR(1)";
            _typeMap[typeof(char?)] = "CHAR(1)";

            _typeMap[typeof(DateTime)] = "TIMESTAMP WITHOUT TIME ZONE";
            _typeMap[typeof(DateTime?)] = "TIMESTAMP WITHOUT TIME ZONE";
            _typeMap[typeof(DateTimeOffset)] = "TIMESTAMP WITH TIME ZONE";
            _typeMap[typeof(DateTimeOffset?)] = "TIMESTAMP WITH TIME ZONE";
            _typeMap[typeof(DateOnly)] = "DATE";
            _typeMap[typeof(DateOnly?)] = "DATE";
            _typeMap[typeof(TimeOnly)] = "TIME";
            _typeMap[typeof(TimeOnly?)] = "TIME";

            _typeMap[typeof(bool)] = "BOOLEAN";
            _typeMap[typeof(bool?)] = "BOOLEAN";

            _typeMap[typeof(byte[])] = "BYTEA";

            _typeMap[typeof(Guid)] = "UUID";
            _typeMap[typeof(Guid?)] = "UUID";

        }

        public static string GetDatabaseType(Type type, int length = -1, int precision = -1, int scale = -1)
        {
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            if (underlyingType.IsEnum)
            {
                return "INTEGER"; // Store enums as integers
            }

            if (!_typeMap.TryGetValue(type, out var dbType))
            {
                throw new NotSupportedException($"Type {type.Name} is not supported for database mapping.");
            }

            if (type == typeof(string))
            {
                if (length > 0)
                {
                    return $"VARCHAR({length})";
                }
                return "TEXT";
            }

            if (type == typeof(decimal) || type == typeof(decimal?))
            {
                if (precision > 0 && scale >= 0)
                {
                    return $"DECIMAL({precision},{scale})";
                }
                return "DECIMAL(18,2)"; // Default precision
            }

            return dbType;
        }

        public static bool IsNullable(Type type)
        {
            if (!type.IsValueType)
                return true; // Reference types are nullable by default

            return Nullable.GetUnderlyingType(type) != null;
        }


        public static Type GetUnderlyingType(Type type)
        {
            return Nullable.GetUnderlyingType(type) ?? type;
        }
    }
}
