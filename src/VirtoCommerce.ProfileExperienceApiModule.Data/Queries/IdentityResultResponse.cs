using System.Collections.Generic;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries
{
    public class IdentityResultResponse
    {
        public bool Succeeded { get; set; }

        public IList<IdentityErrorInfo> Errors { get; set; }
    }
}
