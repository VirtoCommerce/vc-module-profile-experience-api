using System;
using System.Linq;
using GraphQL;
using GraphQL.Builders;
using GraphQL.Types;
using MediatR;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Model.Search;
using VirtoCommerce.ExperienceApiModule.Core.Extensions;
using VirtoCommerce.ExperienceApiModule.Core.Helpers;
using VirtoCommerce.ExperienceApiModule.Core.Infrastructure;
using VirtoCommerce.ExperienceApiModule.Core.Schemas;
using VirtoCommerce.ExperienceApiModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Organization;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;
using VirtoCommerce.ProfileExperienceApiModule.Data.Extensions;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class ContactType : ExtendableGraphType<ContactAggregate>
    {
        public ContactType(
            IDynamicPropertyResolverService dynamicPropertyResolverService,
            IMediator mediator,
            IMemberAggregateFactory memberAggregateFactory)
        {
            Field(x => x.Contact.FirstName);
            Field(x => x.Contact.LastName);
            Field<DateGraphType>("birthDate", resolve: context => context.Source.Contact.BirthDate.HasValue ? context.Source.Contact.BirthDate.Value.Date : (DateTime?)null);
            Field(x => x.Contact.FullName);
            Field(x => x.Contact.Id);
            Field(x => x.Contact.MemberType);
            Field(x => x.Contact.MiddleName, true);
            Field(x => x.Contact.Name, true);
            Field(x => x.Contact.OuterId, true);
            Field(x => x.Contact.Status, true).Description("Contact status");
            Field(x => x.Contact.About);
            Field<ListGraphType<StringGraphType>>("emails", resolve: x => x.Source.Contact.Emails, description: "List of contact`s emails");

            Field<MemberAddressType>("defaultBillingAddress", description: "Default billing address",
                resolve: context => context.Source.Contact.Addresses.FirstOrDefault(address => address.AddressType == CoreModule.Core.Common.AddressType.Billing && address.IsDefault));

            Field<MemberAddressType>("defaultShippingAddress", description: "Default shipping address",
                resolve: context => context.Source.Contact.Addresses.FirstOrDefault(address => address.AddressType == CoreModule.Core.Common.AddressType.Shipping && address.IsDefault));

            ExtendableField<NonNullGraphType<ListGraphType<DynamicPropertyValueType>>>(
                "dynamicProperties",
                "Contact's dynamic property values",
                QueryArgumentPresets.GetArgumentForDynamicProperties(),
                context => dynamicPropertyResolverService.LoadDynamicPropertyValues(context.Source.Contact, context.GetArgumentOrValue<string>("cultureName")));
            Field<ListGraphType<UserType>>("securityAccounts", resolve: context => context.Source.Contact.SecurityAccounts);
            Field<StringGraphType>("organizationId", resolve: context => context.Source.Contact.Organizations?.FirstOrDefault());
            Field("organizationsIds", x => x.Contact.Organizations);
            Field("phones", x => x.Contact.Phones);

            var organizationsConnectionBuilder = GraphTypeExtenstionHelper.CreateConnection<OrganizationType, ContactAggregate>()
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

                return new PagedConnection<OrganizationAggregate>(response.Results.Select(x => memberAggregateFactory.Create<OrganizationAggregate>(x)), query.Skip, query.Take, response.TotalCount);
            });
            AddField(organizationsConnectionBuilder.FieldType);

            var addressesConnectionBuilder = GraphTypeExtenstionHelper.CreateConnection<MemberAddressType, ContactAggregate>()
                .Name("addresses")
                .Argument<StringGraphType>("sort", "Sort expression")
                .PageSize(20);

            addressesConnectionBuilder.Resolve(ResolveAddressesConnection);
            AddField(addressesConnectionBuilder.FieldType);
        }

        private static object ResolveAddressesConnection(IResolveConnectionContext<ContactAggregate> context)
        {
            var take = context.First ?? 20;
            var skip = Convert.ToInt32(context.After ?? 0.ToString());
            var sort = context.GetArgument<string>("sort");
            var addresses = context.Source.Contact.Addresses.AsEnumerable();

            if (!string.IsNullOrEmpty(sort))
            {
                var sortInfos = SortInfo.Parse(sort);
                addresses = addresses
                    .AsQueryable()
                    .OrderBySortInfos(sortInfos);
            }

            return new PagedConnection<Address>(addresses.Skip(skip).Take(take), skip, take, addresses.Count());
        }
    }
}
