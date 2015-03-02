using System.Linq.Expressions;

namespace Neurotoxin.Norm.Query
{
    public class CreateTableExpression : Expression, IHasColumnsExpression
    {
        public TableExpression Table { get; set; }
        public Expression Columns { get; set; }
        public Expression Constraints { get; set; }

        public CreateTableExpression(TableExpression table)
        {
            Table = table;
        }

        public void AddConstraint(Expression constraint)
        {
            Constraints = Constraints == null ? constraint : new ListingExpression(Constraints, constraint);
        }
    }
}