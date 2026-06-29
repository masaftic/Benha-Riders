using BenhaScooters.IntegrationTests.Fixtures;

namespace BenhaScooters.IntegrationTests;

[CollectionDefinition("db")]
public class DatabaseCollection : ICollectionFixture<TestFixture>;
