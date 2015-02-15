using System.Linq.Expressions;

namespace Neurotoxin.Norm.Query
{
    public class SelectExpression : Expression
    {
        public Expression Selection { get; set; }
        public Expression From { get; set; }
        public Expression Where { get; set; }

        public SelectExpression(Expression selection = null, Expression from = null)
        {
            Selection = selection;
            From = from;
        }
    }
}
