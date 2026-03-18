using Npgsql;
using ORM.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ORM.Querying
{
    public class QueryExecutor
    {
        private readonly DatabaseContext _context;

        public QueryExecutor(DatabaseContext context)
        {
            _context = context;
        }

        public List<T> ExecuteQuery<T>(string sql, Dictionary<string, object>? parameters = null) where T : class, new()
        {
            var results = new List<T>();
            var metadata = EntityMapper.GetMetadata(typeof(T));

            var command = CreateCommand(sql, parameters);
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var entity = MaterializeEntity<T>(reader, metadata);
                    results.Add(entity);
                }
            }

            return results;
        }

        public object? ExecuteScalar(string sql, Dictionary<string, object>? parameters = null)
        {
            var command = CreateCommand(sql, parameters);
            return command.ExecuteScalar();
        }

        public int ExecuteNonQuery(string sql, Dictionary<string, object>? parameters = null)
        {
            var command = CreateCommand(sql, parameters);
            return command.ExecuteNonQuery();
        }

        private NpgsqlCommand CreateCommand(string sql, Dictionary<string, object>? parameters)
        {
            var connection = _context.GetConnection();
            var command = new NpgsqlCommand(sql, connection);

            if (_context.CurrentTransaction != null)
            {
                command.Transaction = _context.CurrentTransaction;
            }

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    var value = param.Value;

                    // Npgsql doesn't know how to serialize CLR enums — convert to int
                    if (value != null && value is not DBNull && value.GetType().IsEnum)
                    {
                        value = Convert.ToInt32(value);
                    }

                    command.Parameters.AddWithValue(param.Key, value ?? DBNull.Value);
                }
            }

            return command;
        }

        private T MaterializeEntity<T>(NpgsqlDataReader reader, EntityMetadata metadata) where T : class, new()
        {
            var entity = new T();

            foreach (var property in metadata.Properties)
            {
                // Skip navigation properties (they're loaded separately)
                if (property.IsNavigationProperty)
                    continue;

                try
                {
                    // Get column ordinal (position in result set)
                    var ordinal = reader.GetOrdinal(property.ColumnName);

                    
                    if (reader.IsDBNull(ordinal))
                    {
                        // Only set null if property is nullable
                        if (property.IsNullable)
                        {
                            var propInfo = metadata.EntityType.GetProperty(property.PropertyName);
                            propInfo?.SetValue(entity, null);
                        }
                        continue;
                    }

                    
                    var value = GetValueFromReader(reader, ordinal, property.PropertyType);

                    
                    var propertyInfo = metadata.EntityType.GetProperty(property.PropertyName);
                    propertyInfo?.SetValue(entity, value);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Error materializing property {property.PropertyName} of type {property.PropertyType.Name}", ex);
                }
            }

            return entity;
        }

        private object? GetValueFromReader(NpgsqlDataReader reader, int ordinal, Type propertyType)
        {
            var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            // Handle enums (stored as integers)
            if (underlyingType.IsEnum)
            {
                var intValue = reader.GetInt32(ordinal);
                return Enum.ToObject(underlyingType, intValue);
            }

            // Handle specific types
            return Type.GetTypeCode(underlyingType) switch
            {
                TypeCode.Int32 => reader.GetInt32(ordinal),
                TypeCode.Int64 => reader.GetInt64(ordinal),
                TypeCode.Int16 => reader.GetInt16(ordinal),
                TypeCode.Byte => reader.GetByte(ordinal),
                TypeCode.Decimal => reader.GetDecimal(ordinal),
                TypeCode.Double => reader.GetDouble(ordinal),
                TypeCode.Single => reader.GetFloat(ordinal),
                TypeCode.String => reader.GetString(ordinal),
                TypeCode.Char => reader.GetChar(ordinal),
                TypeCode.Boolean => reader.GetBoolean(ordinal),
                TypeCode.DateTime => reader.GetDateTime(ordinal),
                _ => GetComplexType(reader, ordinal, underlyingType)
            };
        }

        private object? GetComplexType(NpgsqlDataReader reader, int ordinal, Type type)
        {
            if (type == typeof(DateOnly))
            {
                var dateTime = reader.GetDateTime(ordinal);
                return DateOnly.FromDateTime(dateTime);
            }
            else if (type == typeof(TimeOnly))
            {
                var timeSpan = reader.GetTimeSpan(ordinal);
                return TimeOnly.FromTimeSpan(timeSpan);
            }
            else if (type == typeof(DateTimeOffset))
            {
                return reader.GetFieldValue<DateTimeOffset>(ordinal);
            }
            else if (type == typeof(Guid))
            {
                return reader.GetGuid(ordinal);
            }
            else if (type == typeof(byte[]))
            {
                return reader.GetFieldValue<byte[]>(ordinal);
            }

            throw new NotSupportedException($"Type {type.Name} is not supported for materialization");
        }
    }
}
