using System;

namespace Neurotoxin.Soulshot.Query
{
    public class DeleteQuery<TEntity> : DeleteQuery
    {
        public DeleteQuery() : base(typeof(TEntity))
        {
        }
    }

    public class DeleteQuery : LinqToSqlVisitor<DeleteExpression>
    {
        public DeleteQuery(Type entityType) : base(entityType)
        {
        }

        protected override SqlExpression GetSqlExpressionInner()
        {
            return new DeleteExpression(From) { Where = Where };
        }
    }
}