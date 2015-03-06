using System.Collections.Generic;
using Neurotoxin.Norm.Annotations;

namespace Neurotoxin.Norm.Tests.Models
{
    public class Person
    {
        [Key] public int Id { get; set; }
        public string Name { get; set; }
        public IList<Address> Addresses { get; set; }
    }

    public class Address
    {
        [Key] public int Id { get; set; }
        public string Street { get; set; }
        public City Hometown { get; set; }
        public City CurrentCity { get; set; }
    }

    public class City
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
    }
}