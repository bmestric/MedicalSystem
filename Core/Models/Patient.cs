using Core.Enums;
using System;
using System.Collections.Generic;
//using System.ComponentModel.DataAnnotations;
//using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ORM.Attributes;

namespace Core.Models
{
    [Table("patient")]
    public class Patient
    {
        [PrimaryKey]
        [Column("id")]
        public int Id { get; set; }

        [Column("first_name")]
        public string FirstName { get; set; }

        [Column("last_name")]
        public string LastName { get; set; }

        [Column("oib")]
        public string Oib { get; set; }

        [Column("date_of_birth")]
        public DateOnly DateOfBirth { get; set; }

        [Column("Gender")]
        public Gender Gender { get; set; }

        [Column("residence_address")]
        public string ResidenceAddress { get; set; }

        [Column("permanent_address")]
        public string PermanentAddress { get; set; }
    }
}
