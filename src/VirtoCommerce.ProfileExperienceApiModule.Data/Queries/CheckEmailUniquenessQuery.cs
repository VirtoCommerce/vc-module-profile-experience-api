using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries
{
    public class CheckEmailUniquenessQuery : IQuery<CheckEmailUniquenessResponse>
    {
        public string Email { get; set; }
    }
}
