using ElectronicMedicalRecord.Controllers;
using ElectronicMedicalRecord.Models;
using ElectronicMedicalRecord.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace ElectronicMedicalRecord.Tests
{
    public class MedicalRecordControllerUnitTests
    {
        private readonly Mock<ILogger<MedicalRecordController>> _mockLogger;
        private readonly Mock<IMongoDbService> _mockMongoDbService;
        private readonly Mock<IMongoCollection<Patient>> _mockPatientsCollection;
        private readonly MedicalRecordController _controller;

        public MedicalRecordControllerUnitTests()
        {
            _mockLogger = new Mock<ILogger<MedicalRecordController>>();
            _mockMongoDbService = new Mock<IMongoDbService>();
            _mockPatientsCollection = new Mock<IMongoCollection<Patient>>();

            _mockMongoDbService.Setup(x => x.GetCollection<Patient>("patients"))
                .Returns(_mockPatientsCollection.Object);

            _controller = new MedicalRecordController(_mockLogger.Object, _mockMongoDbService.Object);
        }

        [Fact]
        public async Task Get_ReturnsOkResult_WithListOfPatients()
        {
            // Arrange
            var patients = new List<Patient>
            {
                CreateTestPatient("John", "Doe"),
                CreateTestPatient("Jane", "Smith")
            };

            var mockCursor = new Mock<IAsyncCursor<Patient>>();
            mockCursor.Setup(_ => _.Current).Returns(patients);
            mockCursor.SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            _mockPatientsCollection
                .Setup(x => x.FindAsync(
                    It.IsAny<FilterDefinition<Patient>>(),
                    It.IsAny<FindOptions<Patient, Patient>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            // Act
            var result = await _controller.Get();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedPatients = Assert.IsType<List<Patient>>(okResult.Value);
            Assert.Equal(2, returnedPatients.Count);
        }

        [Fact]
        public async Task Get_WithException_ReturnsInternalServerError()
        {
            // Arrange
            _mockPatientsCollection
                .Setup(x => x.FindAsync(
                    It.IsAny<FilterDefinition<Patient>>(),
                    It.IsAny<FindOptions<Patient, Patient>>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Get();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        [Fact]
        public async Task GetById_WithValidId_ReturnsOkResult()
        {
            // Arrange
            var patientId = "507f1f77bcf86cd799439011";
            var patient = CreateTestPatient("John", "Doe");
            patient.Id = patientId;

            var mockCursor = new Mock<IAsyncCursor<Patient>>();
            mockCursor.Setup(_ => _.Current).Returns(new List<Patient> { patient });
            mockCursor.SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            _mockPatientsCollection
                .Setup(x => x.FindAsync(
                    It.IsAny<FilterDefinition<Patient>>(),
                    It.IsAny<FindOptions<Patient, Patient>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            // Act
            var result = await _controller.Get(patientId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedPatient = Assert.IsType<Patient>(okResult.Value);
            Assert.Equal(patientId, returnedPatient.Id);
            Assert.Equal("John", returnedPatient.FirstName);
        }

        [Fact]
        public async Task GetById_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var patientId = "507f1f77bcf86cd799439011";

            var mockCursor = new Mock<IAsyncCursor<Patient>>();
            mockCursor.Setup(_ => _.Current).Returns(new List<Patient>());
            mockCursor.SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _mockPatientsCollection
                .Setup(x => x.FindAsync(
                    It.IsAny<FilterDefinition<Patient>>(),
                    It.IsAny<FindOptions<Patient, Patient>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            // Act
            var result = await _controller.Get(patientId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task Post_WithValidPatient_ReturnsCreatedResult()
        {
            // Arrange
            var patient = CreateTestPatient("Alice", "Johnson");

            _mockPatientsCollection
                .Setup(x => x.InsertOneAsync(
                    It.IsAny<Patient>(),
                    It.IsAny<InsertOneOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Callback<Patient, InsertOneOptions, CancellationToken>((p, options, token) =>
                {
                    p.Id = "507f1f77bcf86cd799439012";
                });

            // Act
            var result = await _controller.Post(patient);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var returnedPatient = Assert.IsType<Patient>(createdResult.Value);
            Assert.Equal("Alice", returnedPatient.FirstName);
            Assert.Equal("Johnson", returnedPatient.LastName);
        }

        [Fact]
        public async Task Post_WithNullPatient_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.Post(null);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task Put_WithValidPatient_ReturnsOkResult()
        {
            // Arrange
            var patientId = "507f1f77bcf86cd799439011";
            var existingPatient = CreateTestPatient("John", "Doe");
            existingPatient.Id = patientId;

            var updatedPatient = CreateTestPatient("John", "Updated");

            // Setup existing patient lookup
            var mockCursor = new Mock<IAsyncCursor<Patient>>();
            mockCursor.Setup(_ => _.Current).Returns(new List<Patient> { existingPatient });
            mockCursor.SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            _mockPatientsCollection
                .Setup(x => x.FindAsync(
                    It.IsAny<FilterDefinition<Patient>>(),
                    It.IsAny<FindOptions<Patient, Patient>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            // Setup replace operation
            _mockPatientsCollection
                .Setup(x => x.ReplaceOneAsync(
                    It.IsAny<FilterDefinition<Patient>>(),
                    It.IsAny<Patient>(),
                    It.IsAny<ReplaceOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Mock<ReplaceOneResult>().Object);

            // Act
            var result = await _controller.Put(patientId, updatedPatient);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedPatient = Assert.IsType<Patient>(okResult.Value);
            Assert.Equal(patientId, returnedPatient.Id);
            Assert.Equal("Updated", returnedPatient.LastName);
        }

        [Fact]
        public async Task Put_WithNonExistentPatient_ReturnsNotFound()
        {
            // Arrange
            var patientId = "507f1f77bcf86cd799439011";
            var updatedPatient = CreateTestPatient("John", "Updated");

            // Setup empty result for patient lookup
            var mockCursor = new Mock<IAsyncCursor<Patient>>();
            mockCursor.Setup(_ => _.Current).Returns(new List<Patient>());
            mockCursor.SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _mockPatientsCollection
                .Setup(x => x.FindAsync(
                    It.IsAny<FilterDefinition<Patient>>(),
                    It.IsAny<FindOptions<Patient, Patient>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockCursor.Object);

            // Act
            var result = await _controller.Put(patientId, updatedPatient);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task Put_WithNullPatient_ReturnsBadRequest()
        {
            // Arrange
            var patientId = "507f1f77bcf86cd799439011";

            // Act
            var result = await _controller.Put(patientId, null);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
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