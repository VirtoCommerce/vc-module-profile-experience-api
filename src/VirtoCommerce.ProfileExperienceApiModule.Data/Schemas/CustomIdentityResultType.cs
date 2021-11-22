using GraphQL.Types;
using VirtoCommerce.ProfileExperienceApiModule.Data.Queries;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class CustomIdentityResultType : ObjectGraphType<IdentityResultResponse>
    {
        public CustomIdentityResultType()
        {
            Field(x => x.Succeeded);
            Field<ListGraphType<IdentityErrorInfoType>>("errors", "The errors that occurred during the identity operation.", resolve: context => context.Source.Errors);
        }
    }
}
