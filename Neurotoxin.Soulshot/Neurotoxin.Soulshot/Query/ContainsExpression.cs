using System;
using System.Collections;
using System.Linq.Expressions;

namespace Neurotoxin.Soulshot.Query
{
    public class ContainsExpression : Expression
    {
        public ColumnExpression Column { get; set; }
        public IEnumerable Content { get; set; }
        public bool IsNot { get; set; }
        public override ExpressionType NodeType => ExpressionType.AndAlso;
        public override Type Type => typeof (bool);

        public ContainsExpression(ColumnExpression column, IEnumerable enumerable)
        {
            Column = column;
            Content = enumerable;
        }
    }
}