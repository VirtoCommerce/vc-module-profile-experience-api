using System;
using System.Linq;
using GraphQL;
using GraphQL.Builders;
using GraphQL.Types;
using MediatR;
using VirtoCommerce.CustomerModule.Core.Model;
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
    public class OrganizationType : ExtendableGraphType<OrganizationAggregate>
    {
        public OrganizationType(IMediator mediator, IDynamicPropertyResolverService dynamicPropertyResolverService, IMemberAggregateFactory factory)
        {
            Name = "Organization";
            Description = "Organization info";

            Field(x => x.Organization.Id);
            Field(x => x.Organization.Description, true).Description("Description");
            Field(x => x.Organization.BusinessCategory, true).Description("Business category");
            Field(x => x.Organization.OwnerId, true).Description("Owner id");
            Field(x => x.Organization.ParentId, true).Description("Parent id");
            Field(x => x.Organization.Name, true).Description("Name");
            Field(x => x.Organization.MemberType).Description("Member type");
            Field(x => x.Organization.OuterId, true).Description("Outer id");
            Field(x => x.Organization.Phones, true);
            Field(x => x.Organization.Emails, true);
            Field(x => x.Organization.Groups, true);
            Field(x => x.Organization.SeoObjectType).Description("SEO object type");
            Field(x => x.Organization.Status, true).Description("Organization status");

            Field<MemberAddressType>("defaultBillingAddress", description: "Default billing address",
                resolve: context => context.Source.Organization.Addresses.FirstOrDefault(address => address.AddressType == CoreModule.Core.Common.AddressType.Billing && address.IsDefault));

            Field<MemberAddressType>("defaultShippingAddress", description: "Default shipping address",
                resolve: context => context.Source.Organization.Addresses.FirstOrDefault(address => address.AddressType == CoreModule.Core.Common.AddressType.Shipping && address.IsDefault));

            ExtendableField<NonNullGraphType<ListGraphType<DynamicPropertyValueType>>>(
               "dynamicProperties",
               "Organization's dynamic property values",
                QueryArgumentPresets.GetArgumentForDynamicProperties(),
                context => dynamicPropertyResolverService.LoadDynamicPropertyValues(context.Source.Organization, context.GetArgumentOrValue<string>("cultureName")));

            var connectionBuilder = GraphTypeExtenstionHelper.CreateConnection<ContactType, OrganizationAggregate>()
               .Name("contacts")
               .Argument<StringGraphType>("searchPhrase", "Free text search")
               .Argument<StringGraphType>("sort", "Sort expression")
               .PageSize(20);

            connectionBuilder.ResolveAsync(async context =>
            {
                var query = context.GetSearchMembersQuery<SearchContactsQuery>();
                query.MemberId = context.Source.Organization.Id;
                query.DeepSearch = false;

                var response = await mediator.Send(query);

                return new PagedConnection<ContactAggregate>(response.Results.Select(x => factory.Create<ContactAggregate>(x)), query.Skip, query.Take, response.TotalCount);
            });
            AddField(connectionBuilder.FieldType);

            var addressesConnectionBuilder = GraphTypeExtenstionHelper.CreateConnection<MemberAddressType, OrganizationAggregate>()
                .Name("addresses")
                .Argument<StringGraphType>("sort", "Sort expression")
                .PageSize(20);

            addressesConnectionBuilder.Resolve(ResolveAddressesConnection);
            AddField(addressesConnectionBuilder.FieldType);
        }

        private static object ResolveAddressesConnection(IResolveConnectionContext<OrganizationAggregate> context)
        {
            var take = context.First ?? 20;
            var skip = Convert.ToInt32(context.After ?? 0.ToString());
            var sort = context.GetArgument<string>("sort");
            var addresses = context.Source.Organization.Addresses.AsEnumerable();

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
