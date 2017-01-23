namespace Neurotoxin.Soulshot.Tests.Models
{
    public class Address
    {
        public int Id { get; set; }
        public string Street { get; set; }
        public City Hometown { get; set; }
        public City CurrentCity { get; set; }
    }
}