using System.Collections.Generic;
using System.Linq;
using Neurotoxin.Soulshot.Annotations;

namespace Neurotoxin.Soulshot.Query
{
    public class ColumnMappingCollection
    {
        public ColumnMapping[] Columns { get; }
        public TableAttribute TableDefinition { get; }

        public ColumnMappingCollection(IEnumerable<ColumnMapping> columns, TableAttribute tableDefinition)
        {
            Columns = columns.ToArray();
            TableDefinition = tableDefinition;
        }
    }
}