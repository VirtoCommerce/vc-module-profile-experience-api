using GraphQL.Types;
using VirtoCommerce.Platform.Core.Security;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputApplicationUserLoginType : InputObjectGraphType<ApplicationUserLogin>
    {
        public InputApplicationUserLoginType()
        {
            Field(x => x.LoginProvider);
            Field(x => x.ProviderKey);
        }
    }
}
