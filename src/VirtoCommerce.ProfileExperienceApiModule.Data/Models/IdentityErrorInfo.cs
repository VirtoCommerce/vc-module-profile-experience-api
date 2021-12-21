using Microsoft.AspNetCore.Identity;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Models
{
    public class IdentityErrorInfo : IdentityError
    {
        public int? ErrorParameter { get; set; }
    }
}
