using System.Linq.Expressions;

namespace Neurotoxin.Soulshot.Query
{
    public class ContainsExpression : Expression
    {
        public ColumnExpression Column { get; set; }
        public Expression Content { get; set; }

        public void AddContent(Expression content)
        {
            Content = Content == null ? content : new ListingExpression(Content, content);
        }

    }
}