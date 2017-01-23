using Neurotoxin.Soulshot.Tests.Models;

namespace Neurotoxin.Soulshot.Tests
{
    public class TestContext : DbContext
    {
        public Table<EntityBase> TestTable { get; }

        public TestContext(string connectionString) : base(connectionString)
        {
            TestTable = new Table<EntityBase>(DataEngine);
        }
    }
}