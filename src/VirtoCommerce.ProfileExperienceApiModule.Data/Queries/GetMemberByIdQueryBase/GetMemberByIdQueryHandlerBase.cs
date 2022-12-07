using System.Threading.Tasks;
using System.Threading;
using VirtoCommerce.ExperienceApiModule.Core.Infrastructure;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries;

public abstract class GetMemberByIdQueryHandlerBase<TQuery, TAggregate> : IQueryHandler<TQuery, TAggregate>
    where TQuery: GetMemberByIdQueryBase<TAggregate>
    where TAggregate: MemberAggregateRootBase
{
    private readonly IMemberAggregateRootRepository _aggregateRepository;

    protected GetMemberByIdQueryHandlerBase(IMemberAggregateRootRepository aggregateRepository)
    {
        _aggregateRepository = aggregateRepository;
    }

    public virtual Task<TAggregate> Handle(TQuery request, CancellationToken cancellationToken)
    {
        return _aggregateRepository.GetMemberAggregateRootByIdAsync<TAggregate>(request.Id);
    }
}
