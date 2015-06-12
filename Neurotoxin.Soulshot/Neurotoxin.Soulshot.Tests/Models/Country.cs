using System.Collections.Generic;
using Neurotoxin.Soulshot.Annotations;

namespace Neurotoxin.Soulshot.Tests.Models
{
    public class Country
    {
        [Key]
        public virtual int Id { get; set; }
        public virtual string Name { get; set; }
        public virtual IList<City> Cities { get; set; }
    }
}