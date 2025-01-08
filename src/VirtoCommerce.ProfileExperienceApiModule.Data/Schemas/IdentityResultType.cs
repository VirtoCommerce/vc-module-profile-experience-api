using GraphQL.Types;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class IdentityResultType : ExtendableGraphType<IdentityResult>
    {
        public IdentityResultType()
        {
            Field(x => x.Succeeded);
            Field<ListGraphType<IdentityErrorType>>("errors").Description("The errors that occurred during the identity operation.").Resolve(context => context.Source.Errors);
        }
    }
}
