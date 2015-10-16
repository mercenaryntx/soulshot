using System.Collections;
using System.Linq.Expressions;

namespace Neurotoxin.Soulshot.Query
{
    public class ContainsExpression : Expression
    {
        public ColumnExpression Column { get; set; }
        public Expression Content { get; set; }

        public ContainsExpression(ColumnExpression column, IEnumerable enumerable)
        {
            Column = column;
            foreach (var value in enumerable)
            {
                AddContent(Constant(value));
            }
        }

        public void AddContent(Expression content)
        {
            Content = Content == null ? content : new ListingExpression(Content, content);
        }

    }
}