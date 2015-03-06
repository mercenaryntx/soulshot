using System.Linq.Expressions;

namespace Neurotoxin.Soulshot.Query
{
    public class SqlPartExpression : Expression
    {
        public string Value { get; set; }

        public SqlPartExpression(string value)
        {
            Value = value;
        }
    }
}