using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neurotoxin.Norm.Tests.Models
{
    public class TestContext : DbContext
    {
        public DbSet<EntityBase> TestTable { get; set; } 

        public TestContext(string connectionString) : base(connectionString)
        {
        }
    }
}
