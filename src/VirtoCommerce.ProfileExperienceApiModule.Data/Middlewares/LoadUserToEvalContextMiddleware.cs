using System;
using System.Threading.Tasks;
using AutoMapper;
using PipelineNet.Middleware;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.MarketingModule.Core.Model.Promotions;
using VirtoCommerce.PricingModule.Core.Model;
using VirtoCommerce.TaxModule.Core.Model;
using VirtoCommerce.Xapi.Core.Services;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Middlewares
{
    public class LoadUserToEvalContextMiddleware : IAsyncMiddleware<PromotionEvaluationContext>, IAsyncMiddleware<PriceEvaluationContext>, IAsyncMiddleware<TaxEvaluationContext>
    {
        private readonly IMapper _mapper;
        private readonly IMemberResolver _memberIdResolver;
        private readonly ILoadUserToEvalContextService _loadUserToEvalContextService;

        public LoadUserToEvalContextMiddleware(IMapper mapper, IMemberResolver memberIdResolver, ILoadUserToEvalContextService loadUserToEvalContextService)
        {
            _mapper = mapper;
            _memberIdResolver = memberIdResolver;
            _loadUserToEvalContextService = loadUserToEvalContextService;
        }

        public async Task Run(PromotionEvaluationContext parameter, Func<PromotionEvaluationContext, Task> next)
        {
            if (!string.IsNullOrEmpty(parameter.CustomerId))
            {
                await _loadUserToEvalContextService.SetShopperDataFromMember(parameter, parameter.CustomerId);
            }

            if (!string.IsNullOrEmpty(parameter.OrganizationId))
            {
                await _loadUserToEvalContextService.SetShopperDataFromOrganization(parameter, parameter.OrganizationId);
            }
            await next(parameter);
        }

        public async Task Run(PriceEvaluationContext parameter, Func<PriceEvaluationContext, Task> next)
        {
            if (!string.IsNullOrEmpty(parameter.CustomerId))
            {
                await _loadUserToEvalContextService.SetShopperDataFromMember(parameter, parameter.CustomerId);
            }

            if (!string.IsNullOrEmpty(parameter.OrganizationId))
            {
                await _loadUserToEvalContextService.SetShopperDataFromOrganization(parameter, parameter.OrganizationId);
            }

            await next(parameter);
        }

        public async Task Run(TaxEvaluationContext parameter, Func<TaxEvaluationContext, Task> next)
        {
            if (!string.IsNullOrEmpty(parameter.CustomerId))
            {
                var member = await _memberIdResolver.ResolveMemberByIdAsync(parameter.CustomerId);
                if (member is Contact contact)
                {
                    parameter.Customer = _mapper.Map<Customer>(contact);
                }
            }
            await next(parameter);
        }

    }
}
