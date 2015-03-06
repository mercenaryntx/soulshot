using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Neurotoxin.Soulshot.Query;

namespace Neurotoxin.Soulshot.Tests.Models
{
    public class SampleWrapper : EntityBase
    {
        private HashSet<string> _dirtyProperties = new HashSet<string>();

        private EntityState _state = EntityState.Unchanged;
        public EntityState State
        {
            get {
                return _state == EntityState.Unchanged && _dirtyProperties.Count > 0 ? EntityState.Changed : _state;
            }
            set { _state = value; }
        }

        public new int Id
        {
            get { return base.Id; }
            set
            {
                base.Id = value;
                _dirtyProperties.Add("Id");
            }
        }
    }
}
