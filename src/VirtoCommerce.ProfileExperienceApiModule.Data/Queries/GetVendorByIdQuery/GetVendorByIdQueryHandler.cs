using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.ExperienceApiModule.Core.Pipelines;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Vendor;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries;

public class GetVendorByIdQueryHandler: GetMemberByIdQueryHandlerBase<GetVendorByIdQuery, VendorAggregate>
{
    private readonly IGenericPipelineLauncher _pipeline;

    public GetVendorByIdQueryHandler(IVendorAggregateRootRepository aggregateRepository, IGenericPipelineLauncher pipeline) : base(aggregateRepository)
    {
        _pipeline = pipeline;
    }

    public override Task<VendorAggregate> Handle(GetVendorByIdQuery request, CancellationToken cancellationToken)
    {
        var result = base.Handle(request, cancellationToken);
        _pipeline.Execute(result);
        return result;
    }
}
