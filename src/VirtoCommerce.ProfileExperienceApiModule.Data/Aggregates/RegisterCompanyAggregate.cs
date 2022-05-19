using Microsoft.AspNetCore.Identity;
using VirtoCommerce.Platform.Core.Security;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates
{
    public class RegisterCompanyAggregate
    {
        public virtual CustomerModule.Core.Model.Organization Company { get; set; }
        public virtual CustomerModule.Core.Model.Contact Owner { get; set; }
        public virtual ApplicationUser Account { get; set; }
        public virtual IdentityResult AccountCreationResult { get; set; }
    }
}
