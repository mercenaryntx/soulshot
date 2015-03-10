using System.Collections.Generic;
using Neurotoxin.Soulshot.Annotations;

namespace Neurotoxin.Soulshot.Tests.Models
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
        public Country Country { get; set; }
        public int PostalCode { get; set; }
        public int Lorem { get; set; }
        public int Ipsum { get; set; }
    }

    public class Country
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
    }
}