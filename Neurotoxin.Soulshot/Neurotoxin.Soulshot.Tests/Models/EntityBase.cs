using System;
using Neurotoxin.Soulshot.Annotations;
using Neurotoxin.Soulshot.Query;

namespace Neurotoxin.Soulshot.Tests.Models
{
    public class EntityBase
    {
        [Key]
        public int Id { get; set; }

        [Index(IndexType.Unique)]
        public Guid EntityId { get; set; }

        public string Name { get; set; }
    }
}