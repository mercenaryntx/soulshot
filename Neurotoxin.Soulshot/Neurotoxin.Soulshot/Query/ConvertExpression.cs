using System;
using System.Linq.Expressions;
using Neurotoxin.Soulshot.Annotations;

namespace Neurotoxin.Soulshot.Query
{
    public class ConvertExpression : Expression
    {
        public ColumnTypeAttribute ToType { get; private set; }
        public ColumnExpression Column { get; private set; }

        public ConvertExpression(ColumnTypeAttribute toType, ColumnExpression column, Type type)
            : base(ExpressionType.Default, type)
        {
            ToType = toType;
            Column = column;
        }
    }
}