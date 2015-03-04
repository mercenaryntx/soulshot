using System.Linq.Expressions;

namespace Neurotoxin.Norm.Query
{
    public class LikeExpression : Expression
    {
        public ColumnExpression Column { get; set; }
        public ConstantExpression Value { get; set; }
    }
}