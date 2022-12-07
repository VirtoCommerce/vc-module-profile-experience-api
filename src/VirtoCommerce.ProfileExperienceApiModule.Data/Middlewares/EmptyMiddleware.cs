using System;
using System.Threading.Tasks;
using PipelineNet.Middleware;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Vendor;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Middlewares;

public class EmptyMiddleware: IAsyncMiddleware<VendorAggregate>
{
    public async Task Run(VendorAggregate parameter, Func<VendorAggregate, Task> next)
    {
        await next(parameter);
    }
}
