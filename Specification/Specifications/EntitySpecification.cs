using Specification.Models;
using System.Linq.Expressions;

namespace Specification.Specifications
{
    public class EntitySpecification<TEntityBase> : BaseSpecification<TEntityBase, EntitySpecification<TEntityBase>> where TEntityBase : BaseEntity
    {
        private EntitySpecification(Expression<Func<TEntityBase, bool>> expression) => Criteria = expression;

        public static EntitySpecification<TEntityBase> ByIds(IList<int> ids) => new EntitySpecification<TEntityBase>(s => ids.Contains(s.Id));

        public static EntitySpecification<TEntityBase> ById(int id) => new EntitySpecification<TEntityBase>(s => s.Id == id);
    }
}
