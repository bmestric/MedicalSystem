namespace ORM.Migrations
{
    public class DatabaseColumnInfo
    {
        public string TableName { get; set; } = null!;
        public string ColumnName { get; set; } = null!;
        public string DataType { get; set; } = null!;
        public bool IsNullable { get; set; }
        public string? ColumnDefault { get; set; }
        public int? CharacterMaxLength { get; set; }
        public int? NumericPrecision { get; set; }
        public int? NumericScale { get; set; }
    }

    public class DatabaseForeignKeyInfo
    {
        public string ConstraintName { get; set; } = null!;
        public string TableName { get; set; } = null!;
        public string ColumnName { get; set; } = null!;
        public string ReferencedTable { get; set; } = null!;
        public string ReferencedColumn { get; set; } = null!;
    }
}
