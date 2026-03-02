using Core.Enums;
using ORM.Attributes;

namespace Core.Models
{
    [Table("patient")]
    public class Patient
    {
        [PrimaryKey(autoIncrement: true)]
        [Column("id")]
        public int Id { get; set; }

        [Column("first_name")]
        [NotNull]
        public string FirstName { get; set; } = null!;

        [Column("last_name")]
        [NotNull]
        public string LastName { get; set; } = null!;

        [Column("oib")]
        [NotNull]
        [Unique]
        public string Oib { get; set; } = null!;

        [Column("date_of_birth")]
        public DateOnly DateOfBirth { get; set; }

        [Column("gender")]
        public Gender Gender { get; set; }

        [Column("residence_address")]
        public string ResidenceAddress { get; set; } = null!;

        [Column("permanent_address")]
        public string PermanentAddress { get; set; } = null!;

    }
}
