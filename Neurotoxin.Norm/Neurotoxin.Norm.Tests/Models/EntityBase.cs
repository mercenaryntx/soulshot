using System;
using Neurotoxin.Norm.Annotations;
using Neurotoxin.Norm.Query;

namespace Neurotoxin.Norm.Tests.Models
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