using System.Linq.Expressions;

namespace Neurotoxin.Soulshot.Query
{
    public interface IHasColumnsExpression
    {
        Expression Columns { get; set; }
    }
}