using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neurotoxin.Soulshot.Tests.Models
{
    public class ClassA : EntityBase
    {
        public virtual int NumberOfSomething { get; set; }
        public virtual DateTime CreatedOn { get; set; }
    }
}