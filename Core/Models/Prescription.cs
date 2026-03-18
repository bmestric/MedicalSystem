using Core.Enums;
using ORM.Attributes;
using System;

namespace Core.Models
{
    [Table("prescription")]
    public class Prescription
    {
        [PrimaryKey(autoIncrement: true)]
        [Column("id")]
        public int Id { get; set; }

        [Column("patient_id")]
        [ForeignKey("patient")]
        [NotNull]
        public int PatientId { get; set; }

        [Column("medication_id")]
        [ForeignKey("medication")]
        [NotNull]
        public int MedicationId { get; set; }

        [Column("doctor_id")]
        [ForeignKey("doctor")]
        [NotNull]
        public int DoctorId { get; set; }

        [Column("dose_amount")]
        [NotNull]
        public decimal DoseAmount { get; set; }

        [Column("dose_unit")]
        [NotNull]
        public DoseUnit DoseUnit { get; set; }

        [Column("frequency")]
        [NotNull]
        public FrequencyType Frequency { get; set; }

        [Column("start_date")]
        [NotNull]
        public DateOnly StartDate { get; set; }

        [Column("end_date")]
        public DateOnly? EndDate { get; set; }

        [NavigationProperty(NavigationType.ManyToOne, ForeignKey = nameof(PatientId))]
        public Patient? Patient { get; set; }

        [NavigationProperty(NavigationType.ManyToOne, ForeignKey = nameof(MedicationId))]
        public Medication? Medication { get; set; }

        [NavigationProperty(NavigationType.ManyToOne, ForeignKey = nameof(DoctorId))]
        public Doctor? Doctor { get; set; }
    }
}
