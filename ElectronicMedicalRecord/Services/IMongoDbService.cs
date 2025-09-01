using MongoDB.Driver;

namespace ElectronicMedicalRecord.Services
{
    public interface IMongoDbService
    {
        IMongoDatabase Database { get; }
        IMongoCollection<T> GetCollection<T>(string collectionName);
    }
}