using System.Linq;
using System.Threading.Tasks;
using GraphQL.Types;
using MediatR;
using VirtoCommerce.CustomerModule.Core.Model.Search;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Organization;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;
using VirtoCommerce.ProfileExperienceApiModule.Data.Extensions;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Helpers;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Xapi.Core.Services;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas;

public class ContactType : MemberBaseType<ContactAggregate>
{
    public ContactType(
        IStoreService storeService,
        IDynamicPropertyResolverService dynamicPropertyResolverService,
        IMemberAddressService memberAddressService,
        IMediator mediator,
        IMemberAggregateFactory memberAggregateFactory)
        : base(storeService, dynamicPropertyResolverService, memberAddressService)
    {
        Field(x => x.Contact.FirstName);
        Field(x => x.Contact.LastName);
        Field(x => x.Contact.MiddleName, true);
        Field(x => x.Contact.FullName);

        Field(x => x.Contact.About);

        Field(x => x.Contact.DefaultLanguage, nullable: true)
            .ResolveAsync(async context =>
                context.Source.Contact.DefaultLanguage ?? (await GetStore(context, storeService))?.DefaultLanguage);
        Field(x => x.Contact.CurrencyCode, nullable: true)
            .ResolveAsync(async context =>
                context.Source.Contact.CurrencyCode ?? (await GetStore(context, storeService))?.DefaultCurrency);

        Field<DateGraphType>("birthDate")
            .Resolve(context =>
                context.Source.Contact.BirthDate.HasValue ? context.Source.Contact.BirthDate.Value.Date : null);

        Field<ListGraphType<UserType>>("securityAccounts").Resolve(context => context.Source.Contact.SecurityAccounts);

        Field<StringGraphType>("organizationId")
            .Resolve(context => context.GetCurrentOrganizationId());

        Field(x => x.Contact.SelectedAddressId, nullable: true).Description("Selected shipping address id.");

        #region Organizations

        Field("organizationsIds", x => x.Contact.Organizations);

        var organizationsConnectionBuilder = GraphTypeExtensionHelper
            .CreateConnection<OrganizationType, ContactAggregate>("organizations")
            .Argument<StringGraphType>("searchPhrase", "Free text search")
            .Argument<StringGraphType>("sort", "Sort expression")
            .PageSize(20);

        organizationsConnectionBuilder.ResolveAsync(async context =>
        {
            var response = AbstractTypeFactory<MemberSearchResult>.TryCreateInstance();
            var query = context.GetSearchMembersQuery<SearchOrganizationsQuery>();

            // If user have no organizations, member search service would return all organizations
            // it means we don't need the search request when user's organization list is empty
            if (!context.Source.Contact.Organizations.IsNullOrEmpty())
            {
                query.DeepSearch = true;
                query.ObjectIds = context.Source.Contact.Organizations;
                response = await mediator.Send(query);
            }

            return new PagedConnection<OrganizationAggregate>(
                response.Results.Select(x => memberAggregateFactory.Create<OrganizationAggregate>(x)), query.Skip,
                query.Take, response.TotalCount);
        });
        AddField(organizationsConnectionBuilder.FieldType);

        #endregion
    }

    private static async Task<Store> GetStore(GraphQL.IResolveFieldContext<ContactAggregate> context, IStoreService storeService)
    {
        return await storeService.GetByIdAsync(context.Source.StoreId);
    }
}
