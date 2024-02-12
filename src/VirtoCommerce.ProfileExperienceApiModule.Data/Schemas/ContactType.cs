using System.Linq;
using GraphQL.Types;
using MediatR;
using VirtoCommerce.CustomerModule.Core.Model.Search;
using VirtoCommerce.ExperienceApiModule.Core.Helpers;
using VirtoCommerce.ExperienceApiModule.Core.Infrastructure;
using VirtoCommerce.ExperienceApiModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Organization;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;
using VirtoCommerce.ProfileExperienceApiModule.Data.Extensions;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas;

public class ContactType : MemberBaseType<ContactAggregate>
{
    public ContactType(
        IDynamicPropertyResolverService dynamicPropertyResolverService,
        IMemberAddressService memberAddressService,
        IMediator mediator,
        IMemberAggregateFactory memberAggregateFactory)
        : base(dynamicPropertyResolverService, memberAddressService)
    {
        Field(x => x.Contact.FirstName);
        Field(x => x.Contact.LastName);
        Field(x => x.Contact.MiddleName, true);
        Field(x => x.Contact.FullName);

        Field(x => x.Contact.About);

        Field<DateGraphType>("birthDate",
            resolve: context =>
                context.Source.Contact.BirthDate.HasValue ? context.Source.Contact.BirthDate.Value.Date : null);

        Field<ListGraphType<UserType>>("securityAccounts", resolve: context => context.Source.Contact.SecurityAccounts);

        Field<StringGraphType>("organizationId",
            resolve: context => context.Source.Contact.Organizations?.FirstOrDefault());

        #region Organizations

        Field("organizationsIds", x => x.Contact.Organizations);

        var organizationsConnectionBuilder = GraphTypeExtenstionHelper
            .CreateConnection<OrganizationType, ContactAggregate>()
            .Name("organizations")
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
}
