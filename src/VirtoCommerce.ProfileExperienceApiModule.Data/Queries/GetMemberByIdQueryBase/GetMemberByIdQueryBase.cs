using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries;

public abstract class GetMemberByIdQueryBase<TAggregate> : IQuery<TAggregate>
    where TAggregate : MemberAggregateRootBase
{
    public string Id { get; set; }
}
