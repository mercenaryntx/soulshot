namespace Neurotoxin.Soulshot.Tests.Models
{
    public class City
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Country Country { get; set; }
        public int PostalCode { get; set; }
        public int Lorem { get; set; }
        public int Ipsum { get; set; }
    }
}