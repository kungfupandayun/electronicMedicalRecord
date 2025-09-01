using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ElectronicMedicalRecord.Models
{
    /// <summary>
    /// A class representing a patient contained in the Patient Management System.
    /// </summary>
    public class Patient
    {
        [BsonIgnore]
        private readonly ILogger<Patient> _logger;

        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Birthday { get; set; }
        public string PhoneNumber { get; set; }
        public string StreetAddress { get; set; }
        public string Postcode { get; set; }
        public double Billing { get; set; }
        public string Doctor { get; set; }
        public string MedicalCondition { get; set; }
        public string MedicalUri { get; set; }
        public string ProfilePicturePath { get; set; }
        public List<string> ConditionImage { get; set; }
        public string Comments { get; set; }
        public string Appointments { get; set; }

        public Patient()
        {
            Id = ObjectId.GenerateNewId().ToString();
            ProfilePicturePath = string.Empty;
            ConditionImage = new List<string>();
            Comments = string.Empty;
            Appointments = string.Empty;
        }

        public Patient(
            ILogger<Patient> logger,
            string firstName,
            string lastName,
            string birthday,
            string phoneNumber,
            string streetAddress,
            string postcode,
            double billing,
            string doctor,
            string medicalCondition,
            string medicalUri)
        {
            _logger = logger;
            _logger.LogInformation("Creating A New Patient");

            FirstName = firstName;
            LastName = lastName;
            Birthday = birthday;
            PhoneNumber = phoneNumber;
            StreetAddress = streetAddress;
            Postcode = postcode;
            Billing = billing;
            Doctor = doctor;
            MedicalCondition = medicalCondition;
            MedicalUri = medicalUri;
            Id = ObjectId.GenerateNewId().ToString();
            ProfilePicturePath = string.Empty;
            ConditionImage = new List<string>();
            Comments = string.Empty;
            Appointments = string.Empty;
        }


        public Patient(
            ILogger<Patient> logger,
            string firstName,
            string lastName,
            string birthday,
            string phoneNumber,
            string streetAddress,
            string postcode,
            double billing,
            string doctor,
            string medicalCondition,
            string medicalUri,
            string id,
            string profilePicturePath,
            List<string> conditionImage,
            string comments,
            string appointments)
            : this(logger, firstName, lastName, birthday, phoneNumber, streetAddress, postcode, billing, doctor, medicalCondition, medicalUri)
        {
            Id = id;
            ProfilePicturePath = profilePicturePath;
            ConditionImage = conditionImage;
            Comments = comments;
            Appointments = appointments;
        }

        /// <summary>
        /// Sets all the personal details of a patient.
        /// </summary>
        public void SetData(
            string firstName,
            string lastName,
            string birthday,
            string phoneNumber,
            string streetAddress,
            string postcode,
            double billing,
            string doctor,
            string medicalCondition,
            string medicalUri)
        {
            FirstName = firstName;
            LastName = lastName;
            Birthday = birthday;
            PhoneNumber = phoneNumber;
            StreetAddress = streetAddress;
            Postcode = postcode;
            Billing = billing;
            Doctor = doctor;
            MedicalCondition = medicalCondition;
            MedicalUri = medicalUri;
        }

        public void SetProfilePicture(string profilePicturePath)
        {
            ProfilePicturePath = profilePicturePath;
        }

        public override string ToString()
        {
            return $"{FirstName} {LastName}";
        }
    }
}