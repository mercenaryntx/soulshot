using System.Collections.Generic;

namespace Neurotoxin.Soulshot.Tests.Models
{
    public class Person
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public IList<Address> Addresses { get; set; }
    }
}