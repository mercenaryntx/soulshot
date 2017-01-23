using System;
using Neurotoxin.Soulshot.Query;

namespace Neurotoxin.Soulshot.Tests.Models
{
    public class Sample
    {
        public int Id { get; set; }
        public Guid EntityId { get; set; }
        public string Name { get; set; }
        public DateTime Date { get; set; }
    }
}