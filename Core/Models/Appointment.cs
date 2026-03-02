using System;
using Core.Enums;
using ORM.Attributes;

namespace Core.Models
{
    [Table("appointment")]
    public class Appointment
    {
        [PrimaryKey(autoIncrement: true)]
        [Column("id")]
        public int Id { get; set; }

        [Column("patient_id")]
        [ForeignKey("patient")]
        [NotNull]
        public int PatientId { get; set; }

        [Column("doctor_id")]
        [ForeignKey("doctor")]
        [NotNull]
        public int DoctorId { get; set; }

        [Column("exam_type")]
        [NotNull]
        public ExamType ExamType { get; set; }

        [Column("scheduled_at")]
        [NotNull]
        public DateTime ScheduledAt { get; set; }

        [Column("notes")]
        public string? Notes { get; set; }

        [NavigationProperty(NavigationType.ManyToOne, ForeignKey = nameof(PatientId))]
        public Patient? Patient { get; set; }

        [NavigationProperty(NavigationType.ManyToOne, ForeignKey = nameof(DoctorId))]
        public Doctor? Doctor { get; set; }
    }
}
