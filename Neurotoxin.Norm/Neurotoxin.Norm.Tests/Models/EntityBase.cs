using System;
using Neurotoxin.Norm.Annotations;

namespace Neurotoxin.Norm.Tests.Models
{
    public class EntityBase
    {
        [Key]
        public int Id { get; set; }

        //[Index]
        //public Guid EntityId { get; set; }

        public string Name { get; set; }
    }
}