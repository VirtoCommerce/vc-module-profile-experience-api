using System.Linq;
using GraphQL.Types;
using MediatR;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Organization;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;
using VirtoCommerce.ProfileExperienceApiModule.Data.Extensions;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.Xapi.Core.Helpers;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Xapi.Core.Services;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas;

public class OrganizationType : MemberBaseType<OrganizationAggregate>
{
    public OrganizationType(
        IStoreService storeService,
        IDynamicPropertyResolverService dynamicPropertyResolverService,
        IMemberAddressService memberAddressService,
        IMediator mediator,
        IMemberAggregateFactory factory)
        : base(storeService, dynamicPropertyResolverService, memberAddressService)
    {
        Name = "Organization";
        Description = "Organization info";

        Field(x => x.Organization.Description, true).Description("Description");
        Field(x => x.Organization.BusinessCategory, true).Description("Business category");
        Field(x => x.Organization.OwnerId, true).Description("Owner id");
        Field(x => x.Organization.ParentId, true).Description("Parent id");

        #region Contacts

        var connectionBuilder = GraphTypeExtensionHelper.CreateConnection<ContactType, OrganizationAggregate>("contacts")
            .Argument<StringGraphType>("searchPhrase", "Free text search")
            .Argument<StringGraphType>("sort", "Sort expression")
            .PageSize(20);

        connectionBuilder.ResolveAsync(async context =>
        {
            var query = context.GetSearchMembersQuery<SearchContactsQuery>();
            query.MemberId = context.Source.Organization.Id;
            query.DeepSearch = false;

            var response = await mediator.Send(query);

            return new PagedConnection<ContactAggregate>(
                response.Results.Select(x => factory.Create<ContactAggregate>(x)), query.Skip, query.Take,
                response.TotalCount);
        });
        AddField(connectionBuilder.FieldType);

        #endregion
    }
}
