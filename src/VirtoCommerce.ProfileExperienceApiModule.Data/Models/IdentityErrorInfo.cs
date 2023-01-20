using System;
using Microsoft.AspNetCore.Identity;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Models
{
    public class IdentityErrorInfo : IdentityError
    {
        public string Parameter { get; set; }
    }
}
