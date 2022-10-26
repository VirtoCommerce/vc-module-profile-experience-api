using AutoMapper;
using GraphQL.Server;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.ExperienceApiModule.Core.Extensions;
using VirtoCommerce.ExperienceApiModule.Core.Infrastructure;
using VirtoCommerce.ExperienceApiModule.Core.Pipelines;
using VirtoCommerce.MarketingModule.Core.Model.Promotions;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.PricingModule.Core.Model;
using VirtoCommerce.ProfileExperienceApiModule.Data;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Organization;
using VirtoCommerce.ProfileExperienceApiModule.Data.Authorization;
using VirtoCommerce.ProfileExperienceApiModule.Data.Middlewares;
using VirtoCommerce.ProfileExperienceApiModule.Data.Schemas;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;
using VirtoCommerce.ProfileExperienceApiModule.Data.Validators;
using VirtoCommerce.TaxModule.Core.Model;

namespace VirtoCommerce.CusomersExperienceApi.Web
{
    public class Module : IModule
    {
        public ManifestModuleInfo ModuleInfo { get; set; }

        public void Initialize(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSchemaBuilder<ProfileSchema>();

            var graphQlbuilder = new CustomGraphQLBuilder(serviceCollection);
            graphQlbuilder.AddGraphTypes(typeof(XProfileAnchor));

            serviceCollection.AddMediatR(typeof(XProfileAnchor));
            serviceCollection.AddSingleton<IMemberAggregateFactory, MemberAggregateFactory>();
            serviceCollection.AddTransient<NewContactValidator>();
            serviceCollection.AddTransient<AccountValidator>();
            serviceCollection.AddTransient<AddressValidator>();
            serviceCollection.AddTransient<OrganizationValidator>();
            serviceCollection.AddTransient<IMemberAggregateRootRepository, MemberAggregateRootRepository>();
            serviceCollection.AddTransient<IOrganizationAggregateRepository, OrganizationAggregateRepository>();
            serviceCollection.AddTransient<IContactAggregateRepository, ContactAggregateRepository>();
            serviceCollection.AddTransient<IAccountService, AccountsService>();
            serviceCollection.AddSingleton<IAuthorizationHandler, ProfileAuthorizationHandler>();

            serviceCollection.AddAutoMapper(typeof(XProfileAnchor));

            serviceCollection.AddPipeline<PromotionEvaluationContext>(builder =>
            {
                builder.AddMiddleware(typeof(LoadUserToEvalContextMiddleware));
            });
            serviceCollection.AddPipeline<TaxEvaluationContext>(builder =>
            {
                builder.AddMiddleware(typeof(LoadUserToEvalContextMiddleware));
            });
            serviceCollection.AddPipeline<PriceEvaluationContext>(builder =>
            {
                builder.AddMiddleware(typeof(LoadUserToEvalContextMiddleware));
            });
        }

        public void PostInitialize(IApplicationBuilder appBuilder)
        {
        }

        public void Uninstall()
        {
            // do nothing in here
        }
    }
}
