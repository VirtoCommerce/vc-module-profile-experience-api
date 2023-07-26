using GraphQL.Server;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.ExperienceApiModule.Core.Extensions;
using VirtoCommerce.ExperienceApiModule.Core.Infrastructure;
using VirtoCommerce.ExperienceApiModule.Core.Pipelines;
using VirtoCommerce.MarketingModule.Core.Model.Promotions;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.PricingModule.Core.Model;
using VirtoCommerce.ProfileExperienceApiModule.Data;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Organization;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Vendor;
using VirtoCommerce.ProfileExperienceApiModule.Data.Authorization;
using VirtoCommerce.ProfileExperienceApiModule.Data.Configuration;
using VirtoCommerce.ProfileExperienceApiModule.Data.Middlewares;
using VirtoCommerce.ProfileExperienceApiModule.Data.Schemas;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;
using VirtoCommerce.ProfileExperienceApiModule.Data.Validators;
using VirtoCommerce.TaxModule.Core.Model;

namespace VirtoCommerce.ProfileExperienceApiModule.Web
{
    public class Module : IModule, IHasConfiguration
    {
        public ManifestModuleInfo ModuleInfo { get; set; }
        public IConfiguration Configuration { get; set; }

        public void Initialize(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSchemaBuilder<ProfileSchema>();

            var graphQlBuilder = new CustomGraphQLBuilder(serviceCollection);
            graphQlBuilder.AddGraphTypes(typeof(XProfileAnchor));

            serviceCollection.AddMediatR(typeof(XProfileAnchor));
            serviceCollection.AddSingleton<IMemberAggregateFactory, MemberAggregateFactory>();
            serviceCollection.AddTransient<NewContactValidator>();
            serviceCollection.AddTransient<AccountValidator>();
            serviceCollection.AddTransient<AddressValidator>();
            serviceCollection.AddTransient<OrganizationValidator>();
            serviceCollection.AddTransient<IMemberAggregateRootRepository, MemberAggregateRootRepository>();
            serviceCollection.AddTransient<IOrganizationAggregateRepository, OrganizationAggregateRepository>();
            serviceCollection.AddTransient<IContactAggregateRepository, ContactAggregateRepository>();
            serviceCollection.AddTransient<IVendorAggregateRepository, VendorAggregateRepository>();
            serviceCollection.AddTransient<IAccountService, AccountsService>();
            serviceCollection.AddSingleton<IAuthorizationHandler, ProfileAuthorizationHandler>();
            serviceCollection.AddOptions<FrontendSecurityOptions>().Bind(Configuration.GetSection("FrontendSecurity")).ValidateDataAnnotations();

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

            serviceCollection.AddPipeline<VendorAggregate>();
        }

        public void PostInitialize(IApplicationBuilder appBuilder)
        {
            var permissionsRegistrar = appBuilder.ApplicationServices.GetRequiredService<IPermissionsRegistrar>();
            permissionsRegistrar.RegisterPermissions(ModuleInfo.Id, "XAPI", ModuleConstants.Security.Permissions.AllPermissions);
        }

        public void Uninstall()
        {
            // Nothing to do there
        }
    }
}
