using System.Linq;
using GraphQL.Types;
using MediatR;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.ExperienceApiModule.Core.Helpers;
using VirtoCommerce.ExperienceApiModule.Core.Infrastructure;
using VirtoCommerce.ExperienceApiModule.Core.Services;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Organization;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;
using VirtoCommerce.ProfileExperienceApiModule.Data.Extensions;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas;

public class OrganizationType : MemberBaseType<OrganizationAggregate>
{
    public OrganizationType(
        IDynamicPropertyResolverService dynamicPropertyResolverService,
        IFavoriteAddressService favoriteAddressService,
        IMediator mediator,
        IMemberAggregateFactory factory)
        : base(dynamicPropertyResolverService, favoriteAddressService)
    {
        Name = "Organization";
        Description = "Organization info";

        Field(x => x.Organization.Description, true).Description("Description");
        Field(x => x.Organization.BusinessCategory, true).Description("Business category");
        Field(x => x.Organization.OwnerId, true).Description("Owner id");
        Field(x => x.Organization.ParentId, true).Description("Parent id");

        #region Contacts

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

            return new PagedConnection<ContactAggregate>(
                response.Results.Select(x => factory.Create<ContactAggregate>(x)), query.Skip, query.Take,
                response.TotalCount);
        });
        AddField(connectionBuilder.FieldType);

        #endregion
    }
}
