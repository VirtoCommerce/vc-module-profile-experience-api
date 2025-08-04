using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputApplicationUserLoginType : ExtendableInputObjectGraphType<ApplicationUserLogin>
    {
        public InputApplicationUserLoginType()
        {
            Field(x => x.LoginProvider);
            Field(x => x.ProviderKey);
        }
    }
}
