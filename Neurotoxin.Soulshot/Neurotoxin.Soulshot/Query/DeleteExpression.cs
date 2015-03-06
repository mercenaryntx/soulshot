using System.Linq.Expressions;

namespace Neurotoxin.Soulshot.Query
{
    public class DeleteExpression : SqlExpression, IHasFromExpression, IHasWhereExpression
    {
        public Expression From { get; set; }
        public Expression Where { get; set; }

        public DeleteExpression(Expression from = null)
        {
            From = from;
        }
    }
}