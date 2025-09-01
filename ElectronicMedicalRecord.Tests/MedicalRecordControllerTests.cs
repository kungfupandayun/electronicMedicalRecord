using System.Net;
using System.Net.Http.Json;
using ElectronicMedicalRecord.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace ElectronicMedicalRecord.Tests
{
    public class MedicalRecordControllerTests : IClassFixture<TestWebApplicationFactory<Program>>
    {
        private readonly TestWebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public MedicalRecordControllerTests(TestWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetAllPatients_ReturnsSuccessAndCorrectContentType()
        {
            // Arrange
            var mockPatients = new List<Patient>
            {
                CreateTestPatient("John", "Doe"),
                CreateTestPatient("Jane", "Smith")
            };

            var mockCursor = new Mock<IAsyncCursor<Patient>>();
            mockCursor.Setup(_ => _.Current).Returns(mockPatients);
            mockCursor.SetupSequence(_ => _.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            mockCursor.SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            _factory.MockPatientsCollection
                .Setup(x => x.FindAsync(
                    It.IsAny<FilterDefinition<Patient>>(),
                    It.IsAny<FindOptions<Patient, Patient>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            // Act
            var response = await _client.GetAsync("/MedicalRecord");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("application/json; charset=utf-8", 
                response.Content.Headers.ContentType?.ToString());

            var patients = await response.Content.ReadFromJsonAsync<List<Patient>>();
            Assert.NotNull(patients);
            Assert.Equal(2, patients.Count);
        }

        [Fact]
        public async Task GetPatientById_WithValidId_ReturnsPatient()
        {
            // Arrange
            var testPatient = CreateTestPatient("John", "Doe");
            var patientId = "507f1f77bcf86cd799439011";
            testPatient.Id = patientId;

            var mockCursor = new Mock<IAsyncCursor<Patient>>();
            mockCursor.Setup(_ => _.Current).Returns(new List<Patient> { testPatient });
            mockCursor.SetupSequence(_ => _.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            mockCursor.SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            _factory.MockPatientsCollection
                .Setup(x => x.FindAsync(
                    It.IsAny<FilterDefinition<Patient>>(),
                    It.IsAny<FindOptions<Patient, Patient>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            // Act
            var response = await _client.GetAsync($"/MedicalRecord/{patientId}");

            // Assert
            response.EnsureSuccessStatusCode();
            var patient = await response.Content.ReadFromJsonAsync<Patient>();
            Assert.NotNull(patient);
            Assert.Equal("John", patient.FirstName);
            Assert.Equal("Doe", patient.LastName);
        }

        [Fact]
        public async Task GetPatientById_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var patientId = "507f1f77bcf86cd799439011";

            var mockCursor = new Mock<IAsyncCursor<Patient>>();
            mockCursor.Setup(_ => _.Current).Returns(new List<Patient>());
            mockCursor.SetupSequence(_ => _.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(false);
            mockCursor.SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _factory.MockPatientsCollection
                .Setup(x => x.FindAsync(
                    It.IsAny<FilterDefinition<Patient>>(),
                    It.IsAny<FindOptions<Patient, Patient>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            // Act
            var response = await _client.GetAsync($"/MedicalRecord/{patientId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CreatePatient_WithValidData_ReturnsCreated()
        {
            // Arrange
            var newPatient = CreateTestPatient("Alice", "Johnson");

            _factory.MockPatientsCollection
                .Setup(x => x.InsertOneAsync(
                    It.IsAny<Patient>(),
                    It.IsAny<InsertOneOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Callback<Patient, InsertOneOptions, CancellationToken>((patient, options, token) =>
                {
                    patient.Id = "507f1f77bcf86cd799439012";
                });

            // Act
            var response = await _client.PostAsJsonAsync("/MedicalRecord", newPatient);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var createdPatient = await response.Content.ReadFromJsonAsync<Patient>();
            Assert.NotNull(createdPatient);
            Assert.Equal("Alice", createdPatient.FirstName);
            Assert.Equal("Johnson", createdPatient.LastName);
        }

        [Fact]
        public async Task CreatePatient_WithNullData_ReturnsBadRequest()
        {
            // Act
            var response = await _client.PostAsJsonAsync("/MedicalRecord", (Patient?)null);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdatePatient_WithValidData_ReturnsOk()
        {
            // Arrange
            var patientId = "507f1f77bcf86cd799439011";
            var existingPatient = CreateTestPatient("John", "Doe");
            existingPatient.Id = patientId;

            var updatedPatient = CreateTestPatient("John", "Updated");
            updatedPatient.Id = patientId;

            // Setup existing patient lookup
            var mockCursor = new Mock<IAsyncCursor<Patient>>();
            mockCursor.Setup(_ => _.Current).Returns(new List<Patient> { existingPatient });
            mockCursor.SetupSequence(_ => _.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
            mockCursor.SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            _factory.MockPatientsCollection
                .Setup(x => x.FindAsync(
                    It.IsAny<FilterDefinition<Patient>>(),
                    It.IsAny<FindOptions<Patient, Patient>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            // Setup replace operation
            _factory.MockPatientsCollection
                .Setup(x => x.ReplaceOneAsync(
                    It.IsAny<FilterDefinition<Patient>>(),
                    It.IsAny<Patient>(),
                    It.IsAny<ReplaceOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Mock<ReplaceOneResult>().Object);

            // Act
            var response = await _client.PutAsJsonAsync($"/MedicalRecord/{patientId}", updatedPatient);

            // Assert
            response.EnsureSuccessStatusCode();
            var patient = await response.Content.ReadFromJsonAsync<Patient>();
            Assert.NotNull(patient);
            Assert.Equal("Updated", patient.LastName);
        }

        [Fact]
        public async Task UpdatePatient_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            var patientId = "507f1f77bcf86cd799439011";
            var updatedPatient = CreateTestPatient("John", "Updated");

            // Setup empty result for patient lookup
            var mockCursor = new Mock<IAsyncCursor<Patient>>();
            mockCursor.Setup(_ => _.Current).Returns(new List<Patient>());
            mockCursor.SetupSequence(_ => _.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(false);
            mockCursor.SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _factory.MockPatientsCollection
                .Setup(x => x.FindAsync(
                    It.IsAny<FilterDefinition<Patient>>(),
                    It.IsAny<FindOptions<Patient, Patient>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            // Act
            var response = await _client.PutAsJsonAsync($"/MedicalRecord/{patientId}", updatedPatient);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        private static Patient CreateTestPatient(string firstName, string lastName)
        {
            return new Patient
            {
                FirstName = firstName,
                LastName = lastName,
                Birthday = "1990-01-01",
                PhoneNumber = "123-456-7890",
                StreetAddress = "123 Test St",
                Postcode = "12345",
                Billing = 100.00,
                Doctor = "Dr. Test",
                MedicalCondition = "Healthy",
                MedicalUri = "http://test.com",
                ProfilePicturePath = "",
                ConditionImage = new List<string>(),
                Comments = "Test patient",
                Appointments = "No appointments"
            };
        }
    }
}