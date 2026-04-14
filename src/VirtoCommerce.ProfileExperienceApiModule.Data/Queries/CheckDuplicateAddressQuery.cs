using VirtoCommerce.ProfileExperienceApiModule.Data.Models;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries
{
    public class CheckDuplicateAddressQuery : IQuery<CheckDuplicateAddressResult>
    {
        public string MemberId { get; set; }
        public MemberAddress Address { get; set; }
    }
}
