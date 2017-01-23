using System;
using Neurotoxin.Soulshot.Annotations;

namespace Neurotoxin.Soulshot.Tests.Models
{
    [Table("EntityBase", MappingStrategy = MappingStrategy.TablePerHierarchy)]
    public class EntityBase
    {
        public int Id { get; set; }
        public Guid EntityId { get; set; }
        public string Name { get; set; }
        public DateTime Date { get; set; }
    }
}