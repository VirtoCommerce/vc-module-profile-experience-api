using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Security;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Models
{
    public class AnonymousUser : ApplicationUser
    {
        public static AnonymousUser Instance => new AnonymousUser();
        private AnonymousUser()
        {
            UserName = "Anonymous";
            Roles = new List<Role>();
        }
    }
}
