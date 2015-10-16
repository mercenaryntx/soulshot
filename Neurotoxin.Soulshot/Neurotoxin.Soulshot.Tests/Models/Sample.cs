using System;
using System.Data.Spatial;
using Neurotoxin.Soulshot.Annotations;
using Neurotoxin.Soulshot.Query;

namespace Neurotoxin.Soulshot.Tests.Models
{
    public class Sample
    {
        [Key]
        public virtual int Id { get; set; }

        [Index(IndexType.Unique)]
        public virtual Guid EntityId { get; set; }

        public virtual string Name { get; set; }

        public virtual DateTime Date { get; set; }

        public virtual DbGeography Location { get; set; }

        //public virtual string Lorem { get; set; }
    }
}