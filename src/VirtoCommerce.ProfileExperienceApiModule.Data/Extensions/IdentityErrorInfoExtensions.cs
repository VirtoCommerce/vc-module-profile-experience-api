using Microsoft.AspNetCore.Identity;
using VirtoCommerce.Platform.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Extensions
{
    public static class IdentityErrorInfoExtensions
    {
        public static IdentityErrorInfo MapToIdentityErrorInfo(this IdentityError x)
        {
            var error = new IdentityErrorInfo { Code = x.Code, Description = x.Description };
            if (x is CustomIdentityError customIdentityError)
            {
                error.ErrorParameter = customIdentityError.ErrorParameter;
            }

            return error;
        }
    }
}
