using System.Linq.Expressions;

namespace Neurotoxin.Norm.Query
{
    public interface IHasColumnsExpression
    {
        Expression Columns { get; set; }
    }
}