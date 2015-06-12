using Neurotoxin.Soulshot.Annotations;

namespace Neurotoxin.Soulshot.Tests.Models
{
    public class City
    {
        [Key]
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
        public virtual Country Country { get; set; }
        public virtual int PostalCode { get; set; }
        public virtual int Lorem { get; set; }
        public virtual int Ipsum { get; set; }
    }
}