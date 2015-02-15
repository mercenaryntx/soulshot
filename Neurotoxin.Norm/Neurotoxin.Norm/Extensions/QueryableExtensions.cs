using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neurotoxin.Norm.Extensions
{
    public static class QueryableExtensions
    {
        public static void Remove<T>(this IQueryable<T> queryable, Func<T,bool> expression)
        {
            
        }
    }
}
