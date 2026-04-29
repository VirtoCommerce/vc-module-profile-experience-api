using System.Collections.Generic;
using GraphQL;
using GraphQL.Types;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries.AddressesQuery;

public abstract class BaseAddressesQuery : SearchQuery<MemberAddressSearchResult>
{
    public string UserId { get; set; }

    public IList<string> CountryCodes { get; set; }

    public IList<string> RegionIds { get; set; }

    public IList<string> Cities { get; set; }


    public override IEnumerable<QueryArgument> GetArguments()
    {
        foreach (var argument in base.GetArguments())
        {
            yield return argument;
        }

        yield return Argument<ListGraphType<StringGraphType>>(nameof(CountryCodes));
        yield return Argument<ListGraphType<StringGraphType>>(nameof(RegionIds));
        yield return Argument<ListGraphType<StringGraphType>>(nameof(Cities));
    }

    public override void Map(IResolveFieldContext context)
    {
        base.Map(context);

        CountryCodes = context.GetArgument<List<string>>(nameof(CountryCodes));
        RegionIds = context.GetArgument<List<string>>(nameof(RegionIds));
        Cities = context.GetArgument<List<string>>(nameof(Cities));

        UserId = context.GetCurrentUserId();
    }
}


