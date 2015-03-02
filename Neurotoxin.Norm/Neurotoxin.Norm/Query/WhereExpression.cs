using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Neurotoxin.Norm.Query
{
    public class WhereExpression : Expression
    {
        public Expression Left { get; private set; }
        public Expression Right { get; private set; }

        public WhereExpression(Expression left, Expression right) : base(ExpressionType.And, null)
        {
            Left = left;
            Right = right;
        }
    }
}