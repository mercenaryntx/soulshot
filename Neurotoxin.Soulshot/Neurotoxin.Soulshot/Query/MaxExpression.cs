using System.Linq.Expressions;

namespace Neurotoxin.Soulshot.Query
{
    public class MaxExpression : Expression
    {
        public Expression Column { get; private set; }

        public MaxExpression(Expression column)
        {
            Column = column;
        }
    }
}