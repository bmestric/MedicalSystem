using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ORM.Attributes;

namespace Core.Models
{
    [Table("medical_history")]
    public class MedicalHistory
    {
        [PrimaryKey(autoIncrement: true)]
        [Column("id")]
        public int Id { get; set; }

        [Column("patient_id")]
        [ForeignKey("patient")]
        [NotNull]
        public int PatientId { get; set; }

        [Column("disease_id")]
        [ForeignKey("disease")]
        [NotNull]
        public int DiseaseId { get; set; }

        [Column("start_date")]
        [NotNull]
        public DateOnly StartDate { get; set; }

        [Column("end_date")]
        public DateOnly? EndDate { get; set; }

        [NavigationProperty(NavigationType.ManyToOne, ForeignKey = nameof(PatientId))]
        public Patient? Patient { get; set; }

        [NavigationProperty(NavigationType.ManyToOne, ForeignKey = nameof(DiseaseId))]
        public Disease? Disease { get; set; }
    }
}
