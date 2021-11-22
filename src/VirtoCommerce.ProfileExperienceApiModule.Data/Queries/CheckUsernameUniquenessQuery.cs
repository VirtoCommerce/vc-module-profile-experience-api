using VirtoCommerce.ExperienceApiModule.Core.Infrastructure;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries
{
    public class CheckUsernameUniquenessQuery : IQuery<CheckUsernameUniquenessResponse>
    {
        public string Username { get; set; }
    }
}
