using ElectronicMedicalRecord.Models;
using MongoDB.Bson;

namespace ElectronicMedicalRecord.Tests
{
    public static class TestDataHelper
    {
        public static Patient CreateTestPatient(
            string firstName = "John",
            string lastName = "Doe",
            string? id = null,
            string birthday = "1990-01-01",
            string phoneNumber = "123-456-7890",
            string streetAddress = "123 Test St",
            string postcode = "12345",
            double billing = 100.00,
            string doctor = "Dr. Test",
            string medicalCondition = "Healthy",
            string medicalUri = "http://test.com")
        {
            return new Patient
            {
                Id = id ?? ObjectId.GenerateNewId().ToString(),
                FirstName = firstName,
                LastName = lastName,
                Birthday = birthday,
                PhoneNumber = phoneNumber,
                StreetAddress = streetAddress,
                Postcode = postcode,
                Billing = billing,
                Doctor = doctor,
                MedicalCondition = medicalCondition,
                MedicalUri = medicalUri,
                ProfilePicturePath = "",
                ConditionImage = new List<string>(),
                Comments = "Test patient",
                Appointments = "No appointments"
            };
        }

        public static List<Patient> CreateMultipleTestPatients(int count)
        {
            var patients = new List<Patient>();
            for (int i = 0; i < count; i++)
            {
                patients.Add(CreateTestPatient(
                    firstName: $"FirstName{i}",
                    lastName: $"LastName{i}",
                    phoneNumber: $"123-456-789{i}",
                    billing: 100.00 + (i * 10)
                ));
            }
            return patients;
        }

        public static Patient CreatePatientWithMinimalData()
        {
            return new Patient
            {
                Id = ObjectId.GenerateNewId().ToString(),
                FirstName = "Min",
                LastName = "Patient",
                Birthday = "1990-01-01",
                PhoneNumber = "",
                StreetAddress = "",
                Postcode = "",
                Billing = 0,
                Doctor = "",
                MedicalCondition = "",
                MedicalUri = "",
                ProfilePicturePath = "",
                ConditionImage = new List<string>(),
                Comments = "",
                Appointments = ""
            };
        }

        public static Patient CreatePatientWithMaximalData()
        {
            return new Patient
            {
                Id = ObjectId.GenerateNewId().ToString(),
                FirstName = "Maximal",
                LastName = "TestPatient",
                Birthday = "1985-05-15",
                PhoneNumber = "+1-555-123-4567",
                StreetAddress = "123 Main Street, Apartment 4B",
                Postcode = "12345-6789",
                Billing = 2500.75,
                Doctor = "Dr. Sarah Johnson, MD",
                MedicalCondition = "Hypertension, Diabetes Type 2",
                MedicalUri = "https://medical-records.example.com/patient/12345",
                ProfilePicturePath = "/images/patient-photos/patient-12345.jpg",
                ConditionImage = new List<string> { 
                    "/images/xrays/xray1.jpg", 
                    "/images/lab-results/blood-test.pdf" 
                },
                Comments = "Patient has been compliant with medication. Regular checkups recommended.",
                Appointments = "Next appointment: 2024-02-15 at 10:00 AM with Dr. Johnson"
            };
        }

        public static string GenerateValidObjectId()
        {
            return ObjectId.GenerateNewId().ToString();
        }

        public static string GetInvalidObjectId()
        {
            return "invalid-object-id";
        }

        public static class SampleData
        {
            public static readonly Patient JohnDoe = CreateTestPatient(
                firstName: "John",
                lastName: "Doe",
                id: "507f1f77bcf86cd799439011",
                birthday: "1985-03-15",
                phoneNumber: "555-0123",
                streetAddress: "123 Oak Street",
                postcode: "10001",
                billing: 1250.00,
                doctor: "Dr. Smith",
                medicalCondition: "Hypertension",
                medicalUri: "http://medical.example.com/john-doe"
            );

            public static readonly Patient JaneSmith = CreateTestPatient(
                firstName: "Jane",
                lastName: "Smith",
                id: "507f1f77bcf86cd799439012",
                birthday: "1990-07-22",
                phoneNumber: "555-0456",
                streetAddress: "456 Pine Avenue",
                postcode: "10002",
                billing: 875.50,
                doctor: "Dr. Johnson",
                medicalCondition: "Allergies",
                medicalUri: "http://medical.example.com/jane-smith"
            );

            public static readonly Patient BobWilson = CreateTestPatient(
                firstName: "Bob",
                lastName: "Wilson",
                id: "507f1f77bcf86cd799439013",
                birthday: "1978-12-03",
                phoneNumber: "555-0789",
                streetAddress: "789 Elm Drive",
                postcode: "10003",
                billing: 2100.25,
                doctor: "Dr. Brown",
                medicalCondition: "Diabetes",
                medicalUri: "http://medical.example.com/bob-wilson"
            );

            public static List<Patient> GetSamplePatients()
            {
                return new List<Patient> { JohnDoe, JaneSmith, BobWilson };
            }
        }
    }
}