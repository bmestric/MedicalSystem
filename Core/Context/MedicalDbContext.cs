using Core.Models;
using ORM.Querying;

namespace Core.Context
{
    public class MedicalDbContext : DatabaseContext
    {
        public DbSet<Patient> Patients => Set<Patient>();
        public DbSet<Doctor> Doctors => Set<Doctor>();
        public DbSet<Disease> Diseases => Set<Disease>();
        public DbSet<MedicalHistory> MedicalHistories => Set<MedicalHistory>();
        public DbSet<Medication> Medications => Set<Medication>();
        public DbSet<Prescription> Prescriptions => Set<Prescription>();
        public DbSet<Appointment> Appointments => Set<Appointment>();

        public MedicalDbContext(string connectionString) : base(connectionString)
        {
        }

        protected override void OnConfiguring()
        {
        }
    }
}
