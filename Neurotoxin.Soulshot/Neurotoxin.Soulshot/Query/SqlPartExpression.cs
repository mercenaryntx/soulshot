using System.Linq.Expressions;

namespace Neurotoxin.Soulshot.Query
{
    public class SqlPartExpression : Expression
    {
        public string Value { get; set; }

        public SqlPartExpression(string value) : base(ExpressionType.Default, typeof(string))
        {
            Value = value;
        }
    }
}