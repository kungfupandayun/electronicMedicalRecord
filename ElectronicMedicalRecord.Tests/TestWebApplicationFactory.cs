using ElectronicMedicalRecord.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using MongoDB.Driver;
using ElectronicMedicalRecord.Models;

namespace ElectronicMedicalRecord.Tests
{
    public class TestWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
    {
        public Mock<IMongoDbService> MockMongoDbService { get; private set; } = new();
        public Mock<IMongoCollection<Patient>> MockPatientsCollection { get; private set; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing MongoDB service registration
                services.RemoveAll(typeof(IMongoDbService));
                
                // Setup mock MongoDB service
                MockMongoDbService.Setup(x => x.GetCollection<Patient>("patients"))
                    .Returns(MockPatientsCollection.Object);
                
                // Register the mock service
                services.AddSingleton(MockMongoDbService.Object);

                // Remove authentication for testing
                services.RemoveAll(typeof(Microsoft.AspNetCore.Authentication.IAuthenticationService));
                services.AddAuthentication("Test")
                    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", options => { });
            });

            builder.UseEnvironment("Testing");
        }

        public void ResetMocks()
        {
            MockMongoDbService.Reset();
            MockPatientsCollection.Reset();
            
            MockMongoDbService.Setup(x => x.GetCollection<Patient>("patients"))
                .Returns(MockPatientsCollection.Object);
        }
    }
}