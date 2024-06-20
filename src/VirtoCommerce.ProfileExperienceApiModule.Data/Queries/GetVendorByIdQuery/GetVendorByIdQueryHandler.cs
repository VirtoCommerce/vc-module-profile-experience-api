using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.Xapi.Core.Pipelines;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Vendor;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries;

public class GetVendorByIdQueryHandler: GetMemberByIdQueryHandlerBase<GetVendorByIdQuery, VendorAggregate>
{
    private readonly IGenericPipelineLauncher _pipeline;

    public GetVendorByIdQueryHandler(IVendorAggregateRepository aggregateRepository, IGenericPipelineLauncher pipeline) : base(aggregateRepository)
    {
        _pipeline = pipeline;
    }

    public override async Task<VendorAggregate> Handle(GetVendorByIdQuery request, CancellationToken cancellationToken)
    {
        var result = await base.Handle(request, cancellationToken);
        await _pipeline.Execute(result);
        return result;
    }
}
