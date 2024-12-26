using GraphQL;
using GraphQL.MicrosoftDI;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Pipelines;

namespace VirtoCommerce.ProfileExperienceApiModule.Web
{
    public class Module : IModule, IHasConfiguration
    {
        public ManifestModuleInfo ModuleInfo { get; set; }
        public IConfiguration Configuration { get; set; }

        public void Initialize(IServiceCollection serviceCollection)
        {
            var graphQlBuilder = new GraphQLBuilder(serviceCollection, builder =>
            {
                var assemblyMarker = typeof(AssemblyMarker);
                builder.AddGraphTypes(assemblyMarker.Assembly);
                serviceCollection.AddMediatR(assemblyMarker);
                serviceCollection.AddAutoMapper(assemblyMarker);
                serviceCollection.AddSchemaBuilders(assemblyMarker);
            });

            // disable scoped schema
            //serviceCollection.AddSingleton<ScopedSchemaFactory<AssemblyMarker>>();

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
            serviceCollection.AddSingleton<IProfileAuthorizationService, ProfileSchema>();
            serviceCollection.AddSingleton<IMemberAddressService, MemberAddressService>();

            serviceCollection.AddOptions<FrontendSecurityOptions>().Bind(Configuration.GetSection("FrontendSecurity")).ValidateDataAnnotations();

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

            // disable scoped schema
            //appBuilder.UseScopedSchema<AssemblyMarker>("profile");
        }

        public void Uninstall()
        {
            // Nothing to do there
        }
    }
}
