using Neurotoxin.Soulshot.Annotations;

namespace Neurotoxin.Soulshot.Tests.Models
{
    public class Address
    {
        [Key]
        public virtual int Id { get; set; }
        public virtual string Street { get; set; }
        public virtual City Hometown { get; set; }
        public virtual City CurrentCity { get; set; }
    }
}