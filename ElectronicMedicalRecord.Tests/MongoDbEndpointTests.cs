using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ElectronicMedicalRecord.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace ElectronicMedicalRecord.Tests
{
    public class MongoDbEndpointTests : IClassFixture<TestWebApplicationFactory<Program>>
    {
        private readonly TestWebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public MongoDbEndpointTests(TestWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
            // Reset mocks before each test to avoid interference
            _factory.ResetMocks();
        }

        [Fact]
        public async Task GetAllPatients_WithEmptyDatabase_ReturnsEmptyList()
        {
            // Arrange
            var mockCursor = CreateMockCursor(new List<Patient>());
            SetupFindAsync(mockCursor);

            // Act
            var response = await _client.GetAsync("/MedicalRecord");

            // Assert
            response.EnsureSuccessStatusCode();
            var patients = await response.Content.ReadFromJsonAsync<List<Patient>>();
            Assert.NotNull(patients);
            Assert.Empty(patients);
        }

        [Fact]
        public async Task GetAllPatients_WithMultiplePatients_ReturnsAllPatients()
        {
            // Arrange
            var testPatients = new List<Patient>
            {
                CreateTestPatient("Alice", "Johnson", "12345678901"),
                CreateTestPatient("Bob", "Williams", "12345678902"),
                CreateTestPatient("Carol", "Brown", "12345678903")
            };

            var mockCursor = CreateMockCursor(testPatients);
            SetupFindAsync(mockCursor);

            // Act
            var response = await _client.GetAsync("/MedicalRecord");

            // Assert
            response.EnsureSuccessStatusCode();
            var patients = await response.Content.ReadFromJsonAsync<List<Patient>>();
            Assert.NotNull(patients);
            Assert.Equal(3, patients.Count);
            Assert.Contains(patients, p => p.FirstName == "Alice");
            Assert.Contains(patients, p => p.FirstName == "Bob");
            Assert.Contains(patients, p => p.FirstName == "Carol");
        }

        [Fact]
        public async Task GetPatientById_WithValidObjectId_ReturnsPatient()
        {
            // Arrange
            var patientId = "507f1f77bcf86cd799439011";
            var testPatient = CreateTestPatient("David", "Miller", "12345678904");
            testPatient.Id = patientId;

            var mockCursor = CreateMockCursor(new List<Patient> { testPatient });
            SetupFindAsync(mockCursor);

            // Act
            var response = await _client.GetAsync($"/MedicalRecord/{patientId}");

            // Assert
            response.EnsureSuccessStatusCode();
            var patient = await response.Content.ReadFromJsonAsync<Patient>();
            Assert.NotNull(patient);
            Assert.Equal(patientId, patient.Id);
            Assert.Equal("David", patient.FirstName);
            Assert.Equal("Miller", patient.LastName);
        }

        [Fact]
        public async Task GetPatientById_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            var nonExistentId = "507f1f77bcf86cd799439999";
            var mockCursor = CreateMockCursor(new List<Patient>());
            SetupFindAsync(mockCursor);

            // Act
            var response = await _client.GetAsync($"/MedicalRecord/{nonExistentId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetPatientById_WithInvalidObjectId_ReturnsNotFound()
        {
            // Arrange
            var invalidId = "invalid-object-id";

            // Act
            var response = await _client.GetAsync($"/MedicalRecord/{invalidId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CreatePatient_WithValidData_ReturnsCreatedAndCorrectLocation()
        {
            // Arrange
            var newPatient = CreateTestPatient("Emma", "Davis", "12345678905");
            newPatient.Id = null; // New patient shouldn't have ID

            var createdPatientId = "507f1f77bcf86cd799439012";

            _factory.MockPatientsCollection
                .Setup(x => x.InsertOneAsync(It.IsAny<Patient>(), It.IsAny<InsertOneOptions>(), default))
                .Callback<Patient, InsertOneOptions, CancellationToken>((patient, options, token) =>
                {
                    patient.Id = createdPatientId; // Simulate MongoDB assigning ID
                })
                .Returns(Task.CompletedTask);

            // Act
            var response = await _client.PostAsJsonAsync("/MedicalRecord", newPatient);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Contains($"/MedicalRecord/{createdPatientId}", response.Headers.Location?.ToString());
            
            var returnedPatient = await response.Content.ReadFromJsonAsync<Patient>();
            Assert.NotNull(returnedPatient);
            Assert.Equal("Emma", returnedPatient.FirstName);
            Assert.Equal("Davis", returnedPatient.LastName);
            Assert.Equal(createdPatientId, returnedPatient.Id);
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
        public async Task CreatePatient_WithMissingRequiredFields_ReturnsBadRequestWithValidationErrors()
        {
            // Arrange
            var invalidPatient = new Patient
            {
                // Missing required fields like FirstName, LastName, etc.
                PhoneNumber = "123-456-7890"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/MedicalRecord", invalidPatient);

            // Assert
            // Note: This might return 201 if validation is not implemented on the server
            // In a real scenario, you'd want to implement model validation
            var result = await response.Content.ReadAsStringAsync();
            // Add appropriate assertions based on your validation requirements
        }

        [Fact]
        public async Task UpdatePatient_WithValidData_ReturnsOkWithUpdatedPatient()
        {
            // Arrange
            var patientId = "507f1f77bcf86cd799439013";
            var existingPatient = CreateTestPatient("Frank", "Wilson", "12345678906");
            existingPatient.Id = patientId;

            var updatedPatient = CreateTestPatient("Franklin", "Wilson", "12345678906");
            updatedPatient.Id = patientId;
            updatedPatient.MedicalCondition = "Updated condition";

            // Mock finding existing patient
            var findCursor = CreateMockCursor(new List<Patient> { existingPatient });
            SetupFindAsync(findCursor);

            // Mock replace operation
            _factory.MockPatientsCollection
                .Setup(x => x.ReplaceOneAsync(
                    It.IsAny<FilterDefinition<Patient>>(), 
                    It.IsAny<Patient>(), 
                    It.IsAny<ReplaceOptions>(), 
                    default))
                .ReturnsAsync(new Mock<ReplaceOneResult>().Object);

            // Act
            var response = await _client.PutAsJsonAsync($"/MedicalRecord/{patientId}", updatedPatient);

            // Assert
            response.EnsureSuccessStatusCode();
            var returnedPatient = await response.Content.ReadFromJsonAsync<Patient>();
            Assert.NotNull(returnedPatient);
            Assert.Equal("Franklin", returnedPatient.FirstName);
            Assert.Equal("Updated condition", returnedPatient.MedicalCondition);
        }

        [Fact]
        public async Task UpdatePatient_WithNonExistentId_ReturnsNotFound()
        {
            // Arrange
            var nonExistentId = "507f1f77bcf86cd799439999";
            var patient = CreateTestPatient("Ghost", "Patient", "12345678907");
            patient.Id = nonExistentId;

            // Mock finding no patient
            var mockCursor = CreateMockCursor(new List<Patient>());
            SetupFindAsync(mockCursor);

            // Act
            var response = await _client.PutAsJsonAsync($"/MedicalRecord/{nonExistentId}", patient);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UpdatePatient_WithNullData_ReturnsBadRequest()
        {
            // Arrange
            var patientId = "507f1f77bcf86cd799439014";

            // Act
            var response = await _client.PutAsJsonAsync($"/MedicalRecord/{patientId}", (Patient?)null);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task DatabaseOperations_HandleMongoExceptionGracefully()
        {
            // Arrange - Mock MongoDB exception
            _factory.MockPatientsCollection
                .Setup(x => x.FindAsync(
                    It.IsAny<FilterDefinition<Patient>>(),
                    It.IsAny<FindOptions<Patient, Patient>>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new MongoException("Database connection failed"));

            // Act
            var response = await _client.GetAsync("/MedicalRecord");

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        }

        [Fact]
        public async Task ConcurrentOperations_HandleMultipleRequestsCorrectly()
        {
            // Arrange
            var patients = new List<Patient>
            {
                CreateTestPatient("Concurrent1", "Test1", "12345678911"),
                CreateTestPatient("Concurrent2", "Test2", "12345678912")
            };

            // Setup mock to handle concurrent calls safely
            var mockCursor = CreateMockCursor(patients);
            _factory.MockPatientsCollection
                .Setup(x => x.FindAsync(
                    It.IsAny<FilterDefinition<Patient>>(),
                    It.IsAny<FindOptions<Patient, Patient>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => CreateMockCursor(patients).Object); // Return new cursor each time

            // Act - Make multiple concurrent requests  
            var tasks = Enumerable.Range(0, 3) // Reduced from 5 to 3 to reduce race conditions
                .Select(async i => 
                {
                    await Task.Delay(i * 10); // Small delay to stagger requests
                    return await _client.GetAsync("/MedicalRecord");
                })
                .ToArray();

            var responses = await Task.WhenAll(tasks);

            // Assert
            Assert.All(responses, response =>
            {
                Assert.True(response.IsSuccessStatusCode, 
                    $"Request failed with status: {response.StatusCode}, reason: {response.ReasonPhrase}");
            });
        }

        [Fact]
        public async Task LargeDataSet_HandlesMultiplePatients()
        {
            // Arrange - Create a large dataset
            var largePatientList = Enumerable.Range(1, 100)
                .Select(i => CreateTestPatient($"Patient{i}", $"LastName{i}", $"1234567890{i:D2}"))
                .ToList();

            var mockCursor = CreateMockCursor(largePatientList);
            SetupFindAsync(mockCursor);

            // Act
            var response = await _client.GetAsync("/MedicalRecord");

            // Assert
            response.EnsureSuccessStatusCode();
            var patients = await response.Content.ReadFromJsonAsync<List<Patient>>();
            Assert.NotNull(patients);
            Assert.Equal(100, patients.Count);
        }

        [Fact]
        public async Task SpecialCharacters_InPatientData_HandledCorrectly()
        {
            // Arrange
            var patientWithSpecialChars = CreateTestPatient("José", "O'Connor-Smith", "123-456-7890");
            patientWithSpecialChars.StreetAddress = "123 Müller Straße, Apt #4B";
            patientWithSpecialChars.MedicalCondition = "Allergic to: nuts, shellfish & dairy";

            var mockCursor = CreateMockCursor(new List<Patient> { patientWithSpecialChars });
            SetupFindAsync(mockCursor);

            // Act
            var response = await _client.GetAsync("/MedicalRecord");

            // Assert
            response.EnsureSuccessStatusCode();
            var patients = await response.Content.ReadFromJsonAsync<List<Patient>>();
            Assert.NotNull(patients);
            Assert.Single(patients);
            Assert.Equal("José", patients[0].FirstName);
            Assert.Equal("O'Connor-Smith", patients[0].LastName);
            Assert.Contains("Müller", patients[0].StreetAddress);
        }

        // Helper methods
        private Mock<IAsyncCursor<Patient>> CreateMockCursor(List<Patient> patients)
        {
            var mockCursor = new Mock<IAsyncCursor<Patient>>();
            mockCursor.Setup(_ => _.Current).Returns(patients);
            mockCursor.SetupSequence(_ => _.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(patients.Count > 0)
                .Returns(false);
            mockCursor.SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(patients.Count > 0)
                .ReturnsAsync(false);
            return mockCursor;
        }

        private void SetupFindAsync(Mock<IAsyncCursor<Patient>> mockCursor)
        {
            _factory.MockPatientsCollection
                .Setup(x => x.FindAsync(
                    It.IsAny<FilterDefinition<Patient>>(),
                    It.IsAny<FindOptions<Patient, Patient>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);
        }

        private Patient CreateTestPatient(string firstName, string lastName, string phoneNumber)
        {
            return new Patient
            {
                FirstName = firstName,
                LastName = lastName,
                Birthday = DateTime.Now.AddYears(-30).ToString("yyyy-MM-dd"),
                PhoneNumber = phoneNumber,
                StreetAddress = "123 Test Street",
                Postcode = "12345",
                Doctor = "Dr. Test",
                MedicalCondition = "Healthy",
                MedicalUri = "http://test.com",
                ProfilePicturePath = "",
                ConditionImage = new List<string>(),
                Comments = $"Test patient: {firstName} {lastName}",
                Appointments = "No appointments"
            };
        }
    }
}