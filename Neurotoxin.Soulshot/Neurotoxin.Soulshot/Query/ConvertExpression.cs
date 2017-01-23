using System;
using System.Linq.Expressions;

namespace Neurotoxin.Soulshot.Query
{
    public class ConvertExpression : Expression
    {
        public string ToType { get; private set; }
        public ColumnExpression Column { get; private set; }

        public ConvertExpression(string toType, ColumnExpression column, Type type) : base(ExpressionType.Default, type)
        {
            ToType = toType;
            Column = column;
        }
    }
}