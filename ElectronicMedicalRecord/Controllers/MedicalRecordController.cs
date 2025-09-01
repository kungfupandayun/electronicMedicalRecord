using ElectronicMedicalRecord.Models;
using ElectronicMedicalRecord.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ElectronicMedicalRecord.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class MedicalRecordController : ControllerBase
    {
        private readonly ILogger<MedicalRecordController> _logger;
        private readonly IMongoDbService _mongoDbService;
        private readonly IMongoCollection<Patient> _patientsCollection;

        public MedicalRecordController(ILogger<MedicalRecordController> logger, IMongoDbService mongoDbService)
        {
            _logger = logger;
            _mongoDbService = mongoDbService;
            _patientsCollection = _mongoDbService.GetCollection<Patient>("patients");
        }

        [HttpGet(Name = "GetAllPatients")]
        public async Task<ActionResult<IEnumerable<Patient>>> Get()
        {
            try
            {
                var patients = await _patientsCollection.Find(_ => true).ToListAsync();
                return Ok(patients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all patients");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}", Name = "GetPatientById")]
        public async Task<ActionResult<Patient>> Get(string id)
        {
            try
            {
                // Validate ObjectId format first
                if (string.IsNullOrWhiteSpace(id) || !ObjectId.TryParse(id, out _))
                {
                    return NotFound($"Patient with ID {id} not found");
                }

                var patient = await _patientsCollection.Find(p => p.Id == id).FirstOrDefaultAsync();
                
                if (patient == null)
                {
                    return NotFound($"Patient with ID {id} not found");
                }
                
                return Ok(patient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving patient with ID: {PatientId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost(Name = "CreatePatient")]
        public async Task<ActionResult<Patient>> Post([FromBody] Patient patient)
        {
            try
            {
                if (patient == null)
                {
                    return BadRequest("Patient data is required");
                }

                // Basic validation
                if (string.IsNullOrWhiteSpace(patient.FirstName) || 
                    string.IsNullOrWhiteSpace(patient.LastName))
                {
                    return BadRequest("FirstName and LastName are required");
                }

                // Ensure no ID is set for new patients
                patient.Id = null;
                await _patientsCollection.InsertOneAsync(patient);
                
                _logger.LogInformation("Created new patient with ID: {PatientId}", patient.Id);
                return CreatedAtAction(nameof(Get), new { id = patient.Id }, patient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating new patient");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}", Name = "UpdatePatient")]
        public async Task<ActionResult<Patient>> Put(string id, [FromBody] Patient patient)
        {
            try
            {
                if (patient == null)
                {
                    return BadRequest("Patient data is required");
                }

                // Validate ObjectId format first
                if (string.IsNullOrWhiteSpace(id) || !ObjectId.TryParse(id, out _))
                {
                    return NotFound($"Patient with ID {id} not found");
                }

                var existingPatient = await _patientsCollection.Find(p => p.Id == id).FirstOrDefaultAsync();
                if (existingPatient == null)
                {
                    return NotFound($"Patient with ID {id} not found");
                }

                patient.Id = id;
                await _patientsCollection.ReplaceOneAsync(p => p.Id == id, patient);
                
                _logger.LogInformation("Updated patient with ID: {PatientId}", id);
                return Ok(patient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating patient with ID: {PatientId}", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
