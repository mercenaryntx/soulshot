using System.ComponentModel;
using System.Linq.Expressions;

namespace Neurotoxin.Norm.Query
{
    public class OrderByExpression : Expression
    {
        public Expression By { get; set; }

        public void AddColumn(Expression column, ListSortDirection direction)
        {
            Expression expression = new ColumnOrderExpression(column, direction);
            //NOTE: intentional switch of branches
            By = By == null ? expression : new ListingExpression(expression, By);
        }
    }
}