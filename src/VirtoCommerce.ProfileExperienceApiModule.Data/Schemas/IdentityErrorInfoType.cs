using GraphQL.Types;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class IdentityErrorInfoType : ObjectGraphType<IdentityErrorInfo>
    {
        public IdentityErrorInfoType()
        {
            Field(x => x.Code).Description("Error code");
            Field(x => x.Parameter, nullable: true).Description("Error parameter");
            Field(x => x.Description, nullable: true).Description("Error description");
        }
    }
}
