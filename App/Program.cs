using Core.Context;
using Core.Enums;
using Core.Models;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var connString = configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

using var context = new MedicalDbContext(connString);

DatabaseInitializer.Initialize(context);
Console.WriteLine("Database initialized.\n");

var entityTypes = new[]
{
    typeof(Patient), typeof(Doctor), typeof(Disease), typeof(Medication),
    typeof(MedicalHistory), typeof(Prescription), typeof(Appointment)
};

var running = true;
while (running)
{
    Console.WriteLine("========== MEDICAL SYSTEM ==========");
    Console.WriteLine("1. Patients");
    Console.WriteLine("2. Diseases");
    Console.WriteLine("3. Medications");
    Console.WriteLine("4. Medical History");
    Console.WriteLine("5. Prescriptions");
    Console.WriteLine("6. Appointments");
    Console.WriteLine("7. View Doctors");
    Console.WriteLine("8. Migrations");
    Console.WriteLine("0. Exit");
    Console.Write("Choose: ");

    switch (Console.ReadLine()?.Trim())
    {
        case "1": ManagePatients(context); break;
        case "2": ManageDiseases(context); break;
        case "3": ManageMedications(context); break;
        case "4": ManageMedicalHistory(context); break;
        case "5": ManagePrescriptions(context); break;
        case "6": ManageAppointments(context); break;
        case "7": ViewDoctors(context); break;
        case "8": ManageMigrations(context, entityTypes); break;
        case "0": running = false; break;
        default: Console.WriteLine("Invalid choice.\n"); break;
    }
}

Console.WriteLine("Goodbye!");

// ==================== MIGRATIONS ====================

static void ManageMigrations(MedicalDbContext context, Type[] entityTypes)
{
    Console.WriteLine("\n--- MIGRATIONS ---");
    Console.WriteLine("1. Preview pending changes");
    Console.WriteLine("2. Create & apply migration");
    Console.WriteLine("3. Rollback last migration");
    Console.WriteLine("4. View migration history");
    Console.Write("Choose: ");

    switch (Console.ReadLine()?.Trim())
    {
        case "1":
            var preview = context.PreviewMigration("preview", entityTypes);
            if (preview.Operations.Count == 0)
            {
                Console.WriteLine("No pending changes. Schema is up to date.\n");
            }
            else
            {
                Console.WriteLine($"\nPending changes ({preview.Operations.Count}):");
                foreach (var op in preview.Operations)
                    Console.WriteLine($"  • {op.Description}");
                Console.WriteLine("\n--- UP SQL ---");
                Console.WriteLine(preview.GetUpSql());
            }
            break;

        case "2":
            Console.Write("Migration name: ");
            var name = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(name)) { Console.WriteLine("Name required.\n"); return; }

            var migration = context.Migrate(name, entityTypes);
            if (migration.Operations.Count == 0)
                Console.WriteLine("No changes to apply.\n");
            break;

        case "3":
            context.Rollback();
            break;

        case "4":
            context.MigrationStatus();
            break;
    }
}

// ==================== PATIENTS ====================

static void ManagePatients(MedicalDbContext context)
{
    Console.WriteLine("\n--- PATIENTS ---");
    Console.WriteLine("1. List all");
    Console.WriteLine("2. Create");
    Console.WriteLine("3. Update");
    Console.WriteLine("4. Delete");
    Console.Write("Choose: ");

    switch (Console.ReadLine()?.Trim())
    {
        case "1":
            ListPatients(context);
            break;
        case "2":
            CreatePatient(context);
            break;
        case "3":
            UpdatePatient(context);
            break;
        case "4":
            DeletePatient(context);
            break;
    }
}

static void ListPatients(MedicalDbContext context)
{
    var patients = context.Patients.ToList();
    if (patients.Count == 0) { Console.WriteLine("No patients found.\n"); return; }

    foreach (var p in patients)
        Console.WriteLine($"  [{p.Id}] {p.FirstName} {p.LastName} | OIB: {p.Oib} | DOB: {p.DateOfBirth:dd.MM.yyyy} | Gender: {p.Gender}");
    Console.WriteLine();
}

static void CreatePatient(MedicalDbContext context)
{
    var patient = new Patient();
    Console.Write("First name: "); patient.FirstName = Console.ReadLine()!;
    Console.Write("Last name: "); patient.LastName = Console.ReadLine()!;
    Console.Write("OIB: "); patient.Oib = Console.ReadLine()!;
    Console.Write("Date of birth (dd.MM.yyyy): "); patient.DateOfBirth = DateOnly.ParseExact(Console.ReadLine()!, "dd.MM.yyyy");
    Console.Write($"Gender ({string.Join(", ", Enum.GetNames<Gender>())}): ");
    patient.Gender = Enum.Parse<Gender>(Console.ReadLine()!, true);
    Console.Write("Residence address: "); patient.ResidenceAddress = Console.ReadLine()!;
    Console.Write("Permanent address: "); patient.PermanentAddress = Console.ReadLine()!;

    context.Patients.Add(patient);
    context.SaveChanges();
    Console.WriteLine($"Created patient ID: {patient.Id}\n");
}

static void UpdatePatient(MedicalDbContext context)
{
    ListPatients(context);
    Console.Write("Enter patient ID to update: ");
    if (!int.TryParse(Console.ReadLine(), out var id)) return;

    var patient = context.Patients.FindById(id);
    if (patient == null) { Console.WriteLine("Not found.\n"); return; }

    Console.Write($"First name [{patient.FirstName}]: ");
    var input = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(input)) patient.FirstName = input;

    Console.Write($"Last name [{patient.LastName}]: ");
    input = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(input)) patient.LastName = input;

    Console.Write($"Residence address [{patient.ResidenceAddress}]: ");
    input = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(input)) patient.ResidenceAddress = input;

    Console.Write($"Permanent address [{patient.PermanentAddress}]: ");
    input = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(input)) patient.PermanentAddress = input;

    context.SaveChanges();
    Console.WriteLine("Patient updated.\n");
}

static void DeletePatient(MedicalDbContext context)
{
    ListPatients(context);
    Console.Write("Enter patient ID to delete: ");
    if (!int.TryParse(Console.ReadLine(), out var id)) return;

    var patient = context.Patients.FindById(id);
    if (patient == null) { Console.WriteLine("Not found.\n"); return; }

    context.Patients.Remove(patient);
    context.SaveChanges();
    Console.WriteLine($"Deleted patient {id}.\n");
}

// ==================== DISEASES ====================

static void ManageDiseases(MedicalDbContext context)
{
    Console.WriteLine("\n--- DISEASES ---");
    Console.WriteLine("1. List all");
    Console.WriteLine("2. Create");
    Console.WriteLine("3. Update");
    Console.WriteLine("4. Delete");
    Console.Write("Choose: ");

    switch (Console.ReadLine()?.Trim())
    {
        case "1": ListDiseases(context); break;
        case "2": CreateDisease(context); break;
        case "3": UpdateDisease(context); break;
        case "4": DeleteDisease(context); break;
    }
}

static void ListDiseases(MedicalDbContext context)
{
    var diseases = context.Diseases.ToList();
    if (diseases.Count == 0) { Console.WriteLine("No diseases found.\n"); return; }

    foreach (var d in diseases)
        Console.WriteLine($"  [{d.Id}] {d.Name} — {d.Description ?? "N/A"}");
    Console.WriteLine();
}

static void CreateDisease(MedicalDbContext context)
{
    var disease = new Disease();
    Console.Write("Name: "); disease.Name = Console.ReadLine()!;
    Console.Write("Description (optional): "); disease.Description = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(disease.Description)) disease.Description = null;

    context.Diseases.Add(disease);
    context.SaveChanges();
    Console.WriteLine($"Created disease ID: {disease.Id}\n");
}

static void UpdateDisease(MedicalDbContext context)
{
    ListDiseases(context);
    Console.Write("Enter disease ID to update: ");
    if (!int.TryParse(Console.ReadLine(), out var id)) return;

    var disease = context.Diseases.FindById(id);
    if (disease == null) { Console.WriteLine("Not found.\n"); return; }

    Console.Write($"Name [{disease.Name}]: ");
    var input = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(input)) disease.Name = input;

    Console.Write($"Description [{disease.Description ?? ""}]: ");
    input = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(input)) disease.Description = input;

    context.SaveChanges();
    Console.WriteLine("Disease updated.\n");
}

static void DeleteDisease(MedicalDbContext context)
{
    ListDiseases(context);
    Console.Write("Enter disease ID to delete: ");
    if (!int.TryParse(Console.ReadLine(), out var id)) return;

    var disease = context.Diseases.FindById(id);
    if (disease == null) { Console.WriteLine("Not found.\n"); return; }

    context.Diseases.Remove(disease);
    context.SaveChanges();
    Console.WriteLine($"Deleted disease {id}.\n");
}

// ==================== MEDICATIONS ====================

static void ManageMedications(MedicalDbContext context)
{
    Console.WriteLine("\n--- MEDICATIONS ---");
    Console.WriteLine("1. List all");
    Console.WriteLine("2. Create");
    Console.WriteLine("3. Update");
    Console.WriteLine("4. Delete");
    Console.Write("Choose: ");

    switch (Console.ReadLine()?.Trim())
    {
        case "1": ListMedications(context); break;
        case "2": CreateMedication(context); break;
        case "3": UpdateMedication(context); break;
        case "4": DeleteMedication(context); break;
    }
}

static void ListMedications(MedicalDbContext context)
{
    var meds = context.Medications.ToList();
    if (meds.Count == 0) { Console.WriteLine("No medications found.\n"); return; }

    foreach (var m in meds)
        Console.WriteLine($"  [{m.Id}] {m.Name} — {m.Description ?? "N/A"}");
    Console.WriteLine();
}

static void CreateMedication(MedicalDbContext context)
{
    var med = new Medication();
    Console.Write("Name: "); med.Name = Console.ReadLine()!;
    Console.Write("Description (optional): "); med.Description = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(med.Description)) med.Description = null;

    context.Medications.Add(med);
    context.SaveChanges();
    Console.WriteLine($"Created medication ID: {med.Id}\n");
}

static void UpdateMedication(MedicalDbContext context)
{
    ListMedications(context);
    Console.Write("Enter medication ID to update: ");
    if (!int.TryParse(Console.ReadLine(), out var id)) return;

    var med = context.Medications.FindById(id);
    if (med == null) { Console.WriteLine("Not found.\n"); return; }

    Console.Write($"Name [{med.Name}]: ");
    var input = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(input)) med.Name = input;

    Console.Write($"Description [{med.Description ?? ""}]: ");
    input = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(input)) med.Description = input;

    context.SaveChanges();
    Console.WriteLine("Medication updated.\n");
}

static void DeleteMedication(MedicalDbContext context)
{
    ListMedications(context);
    Console.Write("Enter medication ID to delete: ");
    if (!int.TryParse(Console.ReadLine(), out var id)) return;

    var med = context.Medications.FindById(id);
    if (med == null) { Console.WriteLine("Not found.\n"); return; }

    context.Medications.Remove(med);
    context.SaveChanges();
    Console.WriteLine($"Deleted medication {id}.\n");
}

// ==================== MEDICAL HISTORY ====================

static void ManageMedicalHistory(MedicalDbContext context)
{
    Console.WriteLine("\n--- MEDICAL HISTORY ---");
    Console.WriteLine("1. List all");
    Console.WriteLine("2. Create");
    Console.WriteLine("3. Update");
    Console.WriteLine("4. Delete");
    Console.Write("Choose: ");

    switch (Console.ReadLine()?.Trim())
    {
        case "1": ListMedicalHistory(context); break;
        case "2": CreateMedicalHistory(context); break;
        case "3": UpdateMedicalHistory(context); break;
        case "4": DeleteMedicalHistory(context); break;
    }
}

static void ListMedicalHistory(MedicalDbContext context)
{
    var records = context.MedicalHistories
        .Include(nameof(MedicalHistory.Patient))
        .Include(nameof(MedicalHistory.Disease))
        .ToList();

    if (records.Count == 0) { Console.WriteLine("No medical history records.\n"); return; }

    foreach (var r in records)
    {
        var patientName = r.Patient != null ? $"{r.Patient.FirstName} {r.Patient.LastName}" : $"PatientID:{r.PatientId}";
        var diseaseName = r.Disease?.Name ?? $"DiseaseID:{r.DiseaseId}";
        var end = r.EndDate.HasValue ? r.EndDate.Value.ToString("dd.MM.yyyy") : "ongoing";
        Console.WriteLine($"  [{r.Id}] {patientName} — {diseaseName} ({r.StartDate:dd.MM.yyyy} to {end})");
    }
    Console.WriteLine();
}

static void CreateMedicalHistory(MedicalDbContext context)
{
    ListPatients(context);
    Console.Write("Patient ID: ");
    if (!int.TryParse(Console.ReadLine(), out var patientId)) return;

    ListDiseases(context);
    Console.Write("Disease ID: ");
    if (!int.TryParse(Console.ReadLine(), out var diseaseId)) return;

    Console.Write("Start date (dd.MM.yyyy): ");
    var startDate = DateOnly.ParseExact(Console.ReadLine()!, "dd.MM.yyyy");

    Console.Write("End date (dd.MM.yyyy, leave empty if ongoing): ");
    var endInput = Console.ReadLine();
    DateOnly? endDate = string.IsNullOrWhiteSpace(endInput) ? null : DateOnly.ParseExact(endInput, "dd.MM.yyyy");

    var record = new MedicalHistory
    {
        PatientId = patientId,
        DiseaseId = diseaseId,
        StartDate = startDate,
        EndDate = endDate
    };

    context.MedicalHistories.Add(record);
    context.SaveChanges();
    Console.WriteLine($"Created medical history ID: {record.Id}\n");
}

static void UpdateMedicalHistory(MedicalDbContext context)
{
    ListMedicalHistory(context);
    Console.Write("Enter medical history ID to update: ");
    if (!int.TryParse(Console.ReadLine(), out var id)) return;

    var record = context.MedicalHistories.FindById(id);
    if (record == null) { Console.WriteLine("Not found.\n"); return; }

    Console.Write($"End date [{record.EndDate?.ToString("dd.MM.yyyy") ?? "ongoing"}] (dd.MM.yyyy, leave empty to skip): ");
    var input = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(input)) record.EndDate = DateOnly.ParseExact(input, "dd.MM.yyyy");

    context.SaveChanges();
    Console.WriteLine("Medical history updated.\n");
}

static void DeleteMedicalHistory(MedicalDbContext context)
{
    ListMedicalHistory(context);
    Console.Write("Enter medical history ID to delete: ");
    if (!int.TryParse(Console.ReadLine(), out var id)) return;

    var record = context.MedicalHistories.FindById(id);
    if (record == null) { Console.WriteLine("Not found.\n"); return; }

    context.MedicalHistories.Remove(record);
    context.SaveChanges();
    Console.WriteLine($"Deleted medical history {id}.\n");
}

// ==================== PRESCRIPTIONS ====================

static void ManagePrescriptions(MedicalDbContext context)
{
    Console.WriteLine("\n--- PRESCRIPTIONS ---");
    Console.WriteLine("1. List all");
    Console.WriteLine("2. Create");
    Console.WriteLine("3. Update");
    Console.WriteLine("4. Delete");
    Console.Write("Choose: ");

    switch (Console.ReadLine()?.Trim())
    {
        case "1": ListPrescriptions(context); break;
        case "2": CreatePrescription(context); break;
        case "3": UpdatePrescription(context); break;
        case "4": DeletePrescription(context); break;
    }
}

static void ListPrescriptions(MedicalDbContext context)
{
    var prescriptions = context.Prescriptions
        .Include(nameof(Prescription.Patient))
        .Include(nameof(Prescription.Medication))
        .Include(nameof(Prescription.Doctor))
        .ToList();

    if (prescriptions.Count == 0) { Console.WriteLine("No prescriptions found.\n"); return; }

    foreach (var rx in prescriptions)
    {
        var patientName = rx.Patient != null ? $"{rx.Patient.FirstName} {rx.Patient.LastName}" : $"PatientID:{rx.PatientId}";
        var medName = rx.Medication?.Name ?? $"MedID:{rx.MedicationId}";
        var doctorName = rx.Doctor != null ? $"Dr. {rx.Doctor.LastName}" : $"DoctorID:{rx.DoctorId}";
        var end = rx.EndDate.HasValue ? rx.EndDate.Value.ToString("dd.MM.yyyy") : "ongoing";
        Console.WriteLine($"  [{rx.Id}] {patientName} — {medName} {rx.DoseAmount} {rx.DoseUnit} ({rx.Frequency}) by {doctorName} | {rx.StartDate:dd.MM.yyyy} to {end}");
    }
    Console.WriteLine();
}

static void CreatePrescription(MedicalDbContext context)
{
    ListPatients(context);
    Console.Write("Patient ID: ");
    if (!int.TryParse(Console.ReadLine(), out var patientId)) return;

    ListMedications(context);
    Console.Write("Medication ID: ");
    if (!int.TryParse(Console.ReadLine(), out var medicationId)) return;

    ViewDoctors(context);
    Console.Write("Doctor ID: ");
    if (!int.TryParse(Console.ReadLine(), out var doctorId)) return;

    Console.Write("Dose amount: ");
    var doseAmount = decimal.Parse(Console.ReadLine()!);

    Console.Write($"Dose unit ({string.Join(", ", Enum.GetNames<DoseUnit>())}): ");
    var doseUnit = Enum.Parse<DoseUnit>(Console.ReadLine()!, true);

    Console.Write($"Frequency ({string.Join(", ", Enum.GetNames<FrequencyType>())}): ");
    var frequency = Enum.Parse<FrequencyType>(Console.ReadLine()!, true);

    Console.Write("Start date (dd.MM.yyyy): ");
    var startDate = DateOnly.ParseExact(Console.ReadLine()!, "dd.MM.yyyy");

    Console.Write("End date (dd.MM.yyyy, leave empty if ongoing): ");
    var endInput = Console.ReadLine();
    DateOnly? endDate = string.IsNullOrWhiteSpace(endInput) ? null : DateOnly.ParseExact(endInput, "dd.MM.yyyy");

    var prescription = new Prescription
    {
        PatientId = patientId,
        MedicationId = medicationId,
        DoctorId = doctorId,
        DoseAmount = doseAmount,
        DoseUnit = doseUnit,
        Frequency = frequency,
        StartDate = startDate,
        EndDate = endDate
    };

    context.Prescriptions.Add(prescription);
    context.SaveChanges();
    Console.WriteLine($"Created prescription ID: {prescription.Id}\n");
}

static void UpdatePrescription(MedicalDbContext context)
{
    ListPrescriptions(context);
    Console.Write("Enter prescription ID to update: ");
    if (!int.TryParse(Console.ReadLine(), out var id)) return;

    var rx = context.Prescriptions.FindById(id);
    if (rx == null) { Console.WriteLine("Not found.\n"); return; }

    Console.Write($"Dose amount [{rx.DoseAmount}]: ");
    var input = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(input)) rx.DoseAmount = decimal.Parse(input);

    Console.Write($"Dose unit [{rx.DoseUnit}] ({string.Join(", ", Enum.GetNames<DoseUnit>())}): ");
    input = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(input)) rx.DoseUnit = Enum.Parse<DoseUnit>(input, true);

    Console.Write($"Frequency [{rx.Frequency}] ({string.Join(", ", Enum.GetNames<FrequencyType>())}): ");
    input = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(input)) rx.Frequency = Enum.Parse<FrequencyType>(input, true);

    Console.Write($"End date [{rx.EndDate?.ToString("dd.MM.yyyy") ?? "ongoing"}] (dd.MM.yyyy, leave empty to skip): ");
    input = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(input)) rx.EndDate = DateOnly.ParseExact(input, "dd.MM.yyyy");

    context.SaveChanges();
    Console.WriteLine("Prescription updated.\n");
}

static void DeletePrescription(MedicalDbContext context)
{
    ListPrescriptions(context);
    Console.Write("Enter prescription ID to delete: ");
    if (!int.TryParse(Console.ReadLine(), out var id)) return;

    var rx = context.Prescriptions.FindById(id);
    if (rx == null) { Console.WriteLine("Not found.\n"); return; }

    context.Prescriptions.Remove(rx);
    context.SaveChanges();
    Console.WriteLine($"Deleted prescription {id}.\n");
}

// ==================== APPOINTMENTS ====================

static void ManageAppointments(MedicalDbContext context)
{
    Console.WriteLine("\n--- APPOINTMENTS ---");
    Console.WriteLine("1. List all");
    Console.WriteLine("2. Create");
    Console.WriteLine("3. Update");
    Console.WriteLine("4. Delete");
    Console.Write("Choose: ");

    switch (Console.ReadLine()?.Trim())
    {
        case "1": ListAppointments(context); break;
        case "2": CreateAppointment(context); break;
        case "3": UpdateAppointment(context); break;
        case "4": DeleteAppointment(context); break;
    }
}

static void ListAppointments(MedicalDbContext context)
{
    var appointments = context.Appointments
        .Include(nameof(Appointment.Patient))
        .Include(nameof(Appointment.Doctor))
        .ToList();

    if (appointments.Count == 0) { Console.WriteLine("No appointments found.\n"); return; }

    foreach (var a in appointments)
    {
        var patientName = a.Patient != null ? $"{a.Patient.FirstName} {a.Patient.LastName}" : $"PatientID:{a.PatientId}";
        var doctorName = a.Doctor != null ? $"Dr. {a.Doctor.LastName}" : $"DoctorID:{a.DoctorId}";
        Console.WriteLine($"  [{a.Id}] {patientName} — {a.ExamType} with {doctorName} at {a.ScheduledAt:dd.MM.yyyy HH:mm} | Notes: {a.Notes ?? "N/A"}");
    }
    Console.WriteLine();
}

static void CreateAppointment(MedicalDbContext context)
{
    ListPatients(context);
    Console.Write("Patient ID: ");
    if (!int.TryParse(Console.ReadLine(), out var patientId)) return;

    ViewDoctors(context);
    Console.Write("Doctor ID: ");
    if (!int.TryParse(Console.ReadLine(), out var doctorId)) return;

    Console.Write($"Exam type ({string.Join(", ", Enum.GetNames<ExamType>())}): ");
    var examType = Enum.Parse<ExamType>(Console.ReadLine()!, true);

    Console.Write("Scheduled at (dd.MM.yyyy HH:mm): ");
    var scheduledAt = DateTime.ParseExact(Console.ReadLine()!, "dd.MM.yyyy HH:mm", null);

    Console.Write("Notes (optional): ");
    var notes = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(notes)) notes = null;

    var appointment = new Appointment
    {
        PatientId = patientId,
        DoctorId = doctorId,
        ExamType = examType,
        ScheduledAt = scheduledAt,
        Notes = notes
    };

    context.Appointments.Add(appointment);
    context.SaveChanges();
    Console.WriteLine($"Created appointment ID: {appointment.Id}\n");
}

static void UpdateAppointment(MedicalDbContext context)
{
    ListAppointments(context);
    Console.Write("Enter appointment ID to update: ");
    if (!int.TryParse(Console.ReadLine(), out var id)) return;

    var appt = context.Appointments.FindById(id);
    if (appt == null) { Console.WriteLine("Not found.\n"); return; }

    Console.Write($"Scheduled at [{appt.ScheduledAt:dd.MM.yyyy HH:mm}] (leave empty to skip): ");
    var input = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(input)) appt.ScheduledAt = DateTime.ParseExact(input, "dd.MM.yyyy HH:mm", null);

    Console.Write($"Notes [{appt.Notes ?? ""}]: ");
    input = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(input)) appt.Notes = input;

    context.SaveChanges();
    Console.WriteLine("Appointment updated.\n");
}

static void DeleteAppointment(MedicalDbContext context)
{
    ListAppointments(context);
    Console.Write("Enter appointment ID to delete: ");
    if (!int.TryParse(Console.ReadLine(), out var id)) return;

    var appt = context.Appointments.FindById(id);
    if (appt == null) { Console.WriteLine("Not found.\n"); return; }

    context.Appointments.Remove(appt);
    context.SaveChanges();
    Console.WriteLine($"Deleted appointment {id}.\n");
}

// ==================== DOCTORS (read-only) ====================

static void ViewDoctors(MedicalDbContext context)
{
    var doctors = context.Doctors.ToList();
    Console.WriteLine("\n--- DOCTORS ---");
    foreach (var d in doctors)
        Console.WriteLine($"  [{d.Id}] {d}");
    Console.WriteLine();
}


