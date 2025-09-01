# Electronic Medical Record System

A comprehensive Electronic Medical Record (EMR) system built with ASP.NET Core 8.0 and MongoDB, providing secure patient management with JWT authentication.

## Features

- **Patient Management**: Complete CRUD operations for patient records
- **Secure Authentication**: JWT-based authentication system
- **MongoDB Integration**: NoSQL database for flexible data storage
- **RESTful API**: Well-documented API endpoints with Swagger/OpenAPI
- **Docker Support**: Containerized deployment ready
- **Medical Data Management**: Store patient information, medical conditions, billing, and appointments

## Technology Stack

- **Framework**: ASP.NET Core 8.0 (.NET 8)
- **Database**: MongoDB
- **Authentication**: JWT Bearer tokens
- **Documentation**: Swagger/OpenAPI
- **Testing**: xUnit (ElectronicMedicalRecord.Tests)
- **Containerization**: Docker

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- MongoDB instance (local or cloud)
- Visual Studio 2022 or Visual Studio Code (optional)

### Installation

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd ElectronicMedicalRecord
   ```

2. **Configure MongoDB**
   
   Update `appsettings.json` with your MongoDB connection details:
   ```json
   {
     "MongoDB": {
       "ConnectionString": "your-mongodb-connection-string",
       "DatabaseName": "your-database-name"
     }
   }
   ```

3. **Configure JWT Settings**
   
   Update `appsettings.json` with your JWT configuration:
   ```json
   {
     "Jwt": {
       "Key": "your-secret-key",
       "Issuer": "your-issuer",
       "Audience": "your-audience"
     }
   }
   ```

4. **Build and run the application**
   ```bash
   dotnet restore
   dotnet build
   dotnet run --project ElectronicMedicalRecord
   ```

5. **Access the application**
   - API: `https://localhost:7xxx` (port may vary)
   - Swagger UI: `https://localhost:7xxx/swagger`

## Project Structure

```
ElectronicMedicalRecord/
├── Controllers/
│   ├── AuthController.cs          # Authentication endpoints
│   └── MedicalRecordController.cs # Patient management endpoints
├── Models/
│   ├── Patient.cs                 # Patient entity model
│   ├── LoginRequest.cs            # Login request model
│   └── LoginResponse.cs           # Login response model
├── Services/
│   ├── IMongoDbService.cs         # MongoDB service interface
│   ├── MongoDbService.cs          # MongoDB service implementation
│   └── MongoDbSettings.cs         # MongoDB configuration
├── Program.cs                     # Application entry point
├── appsettings.json              # Application configuration
└── ElectronicMedicalRecord.csproj # Project file
```

## API Endpoints

### Authentication
- `POST /api/auth/login` - User authentication

### Patient Management
- `GET /api/medicalrecord` - Get all patients
- `GET /api/medicalrecord/{id}` - Get patient by ID
- `POST /api/medicalrecord` - Create new patient
- `PUT /api/medicalrecord/{id}` - Update patient
- `DELETE /api/medicalrecord/{id}` - Delete patient

## Patient Data Model

The Patient model includes:
- Personal information (name, birthday, phone, address)
- Medical details (condition, doctor, medical URI)
- Billing information
- Profile pictures and condition images
- Comments and appointments

## Development

### Running Tests

```bash
dotnet test ElectronicMedicalRecord.Tests
```

### Docker Deployment

The project includes Docker support with multi-stage builds:

```bash
docker build -t emr-system .
docker run -p 8080:80 emr-system
```

### Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add/update tests as needed
5. Submit a pull request

## Security

- JWT token-based authentication
- Secure password handling
- HTTPS enforcement in production
- MongoDB connection security


## Support

For issues and questions, please open an issue in the GitHub repository.