using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using PipelineNet.Middleware;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.MarketingModule.Core.Model.Promotions;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.DynamicProperties;
using VirtoCommerce.PricingModule.Core.Model;
using VirtoCommerce.TaxModule.Core.Model;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Middlewares
{
    public class LoadUserToEvalContextMiddleware : IAsyncMiddleware<PromotionEvaluationContext>, IAsyncMiddleware<PriceEvaluationContext>, IAsyncMiddleware<TaxEvaluationContext>
    {
        private readonly IMapper _mapper;
        private readonly IMemberResolver _memberIdResolver;
        private readonly IMemberService _memberService;

        public LoadUserToEvalContextMiddleware(IMapper mapper, IMemberResolver memberIdResolver, IMemberService memberService)
        {
            _mapper = mapper;
            _memberIdResolver = memberIdResolver;
            _memberService = memberService;
        }

        public async Task Run(PromotionEvaluationContext parameter, Func<PromotionEvaluationContext, Task> next)
        {
            if (!string.IsNullOrEmpty(parameter.CustomerId))
            {
                await InnerSetShopperDataFromMember(parameter, parameter.CustomerId);
            }
            await next(parameter);
        }

        public async Task Run(PriceEvaluationContext parameter, Func<PriceEvaluationContext, Task> next)
        {
            if (!string.IsNullOrEmpty(parameter.CustomerId))
            {
                await InnerSetShopperDataFromMember(parameter, parameter.CustomerId);
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

        private async Task InnerSetShopperDataFromMember(EvaluationContextBase evalContextBase, string customerId)
        {
            var member = await _memberIdResolver.ResolveMemberByIdAsync(customerId);
            if (member is Contact contact)
            {
                evalContextBase.ShopperGender = contact.GetDynamicPropertyValue("gender", string.Empty);

                if (contact.BirthDate != null)
                {
                    var zeroTime = new DateTime(1, 1, 1);
                    var span = DateTime.UtcNow - contact.BirthDate.Value;
                    evalContextBase.ShopperAge = (zeroTime + span).Year - 1;
                }

                evalContextBase.GeoTimeZone = contact.TimeZone;

                evalContextBase.UserGroups = contact.Groups?.ToArray();

                if (!contact.Organizations.IsNullOrEmpty())
                {
                    var userGroups = new List<string>();

                    if (!evalContextBase.UserGroups.IsNullOrEmpty())
                    {
                        userGroups.AddRange(evalContextBase.UserGroups);
                    }

                    var organizations = await _memberService.GetByIdsAsync(contact.Organizations.ToArray(), MemberResponseGroup.WithGroups.ToString());
                    userGroups.AddRange(organizations.OfType<Organization>().SelectMany(x => x.Groups));

                    evalContextBase.UserGroups = userGroups.Distinct().ToArray();
                }
            }
        }
    }
}
