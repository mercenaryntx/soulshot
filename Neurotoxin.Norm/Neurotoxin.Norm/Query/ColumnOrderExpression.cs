using System.ComponentModel;
using System.Linq.Expressions;

namespace Neurotoxin.Norm.Query
{
    public class ColumnOrderExpression : Expression
    {
        public Expression Column { get; set; }
        public ListSortDirection Direction { get; set; }

        public ColumnOrderExpression(Expression column, ListSortDirection direction)
        {
            Column = column;
            Direction = direction;
        }
    }
}