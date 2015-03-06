using System.Linq.Expressions;

namespace Neurotoxin.Soulshot.Query
{
    public class InsertExpression : Expression
    {
        public Expression Into { get; set; }
        public ValuesExpression Values { get; set; }
        public SelectExpression Select { get; set; }
        public bool IsIdentityInsertEnabled { get; set; }

        public InsertExpression(Expression @into = null)
        {
            Into = @into;
        }
    }
}