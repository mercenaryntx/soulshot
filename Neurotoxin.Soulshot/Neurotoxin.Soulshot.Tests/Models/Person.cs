using System.Collections.Generic;
using Neurotoxin.Soulshot.Annotations;

namespace Neurotoxin.Soulshot.Tests.Models
{
    public class Person
    {
        [Key]
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
        public virtual IList<Address> Addresses { get; set; }
    }
}