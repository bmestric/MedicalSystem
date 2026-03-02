using Core.Enums;
using Core.Models;

namespace Core.Context
{
    public static class DatabaseInitializer
    {
        public static void Initialize(MedicalDbContext context)
        {
            // Create tables in dependency order
            context.EnsureCreated(
                typeof(Patient),
                typeof(Doctor),
                typeof(Disease),
                typeof(Medication),
                typeof(MedicalHistory),
                typeof(Prescription),
                typeof(Appointment)
            );

            SeedDoctors(context);
            SeedDiseases(context);
            SeedMedications(context);
        }

        private static void SeedDoctors(MedicalDbContext context)
        {
            var existing = context.Doctors.ToList();
            if (existing.Count > 0)
                return;

            context.Doctors.Add(new Doctor
            {
                FirstName = "Ivan",
                LastName = "Horvat",
                Specialization = "Opæa medicina"
            });
            context.Doctors.Add(new Doctor
            {
                FirstName = "Ana",
                LastName = "Kovaèeviæ",
                Specialization = "Kardiologija"
            });
            context.Doctors.Add(new Doctor
            {
                FirstName = "Marko",
                LastName = "Babiæ",
                Specialization = "Neurologija"
            });
            context.Doctors.Add(new Doctor
            {
                FirstName = "Petra",
                LastName = "Novak",
                Specialization = "Dermatologija"
            });
            context.Doctors.Add(new Doctor
            {
                FirstName = "Tomislav",
                LastName = "Juriæ",
                Specialization = "Radiologija"
            });

            context.SaveChanges();
            Console.WriteLine("Seeded 5 doctors.");
        }

        private static void SeedDiseases(MedicalDbContext context)
        {
            var existing = context.Diseases.ToList();
            if (existing.Count > 0)
                return;

            context.Diseases.Add(new Disease { Name = "Hipertenzija", Description = "Povišeni krvni tlak" });
            context.Diseases.Add(new Disease { Name = "Dijabetes tip 2", Description = "Šeæerna bolest tipa 2" });
            context.Diseases.Add(new Disease { Name = "Astma", Description = "Kronièna upalna bolest dišnih puteva" });
            context.Diseases.Add(new Disease { Name = "Migrena", Description = "Jaki ponavljajuæi glavobolje" });
            context.Diseases.Add(new Disease { Name = "Dermatitis", Description = "Upala kože" });

            context.SaveChanges();
            Console.WriteLine("Seeded 5 diseases.");
        }

        private static void SeedMedications(MedicalDbContext context)
        {
            var existing = context.Medications.ToList();
            if (existing.Count > 0)
                return;

            context.Medications.Add(new Medication { Name = "Aspirin", Description = "Acetilsalicilna kiselina" });
            context.Medications.Add(new Medication { Name = "Metformin", Description = "Antidijabetik" });
            context.Medications.Add(new Medication { Name = "Ventolin", Description = "Bronhodilatator za astmu" });
            context.Medications.Add(new Medication { Name = "Ibuprofen", Description = "Protuupalni lijek" });
            context.Medications.Add(new Medication { Name = "Amlodipin", Description = "Lijek za krvni tlak" });

            context.SaveChanges();
            Console.WriteLine("Seeded 5 medications.");
        }
    }
}
