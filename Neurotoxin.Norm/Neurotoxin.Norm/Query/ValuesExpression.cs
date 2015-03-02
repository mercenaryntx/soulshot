using System.Linq.Expressions;

namespace Neurotoxin.Norm.Query
{
    public class ValuesExpression : Expression, IHasColumnsExpression
    {
        public Expression Columns { get; set; }
        public Expression Values { get; set; }

        public void AddValue(ConstantExpression value)
        {
            Values = Values == null ? (Expression)value : new ListingExpression(Values, value);
        }
    }
}