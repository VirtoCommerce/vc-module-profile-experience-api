namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class SearchOrganizationsQuery : SearchMembersQueryBase
    {
        /// <summary>
        /// When set, organizations where this user has a locked membership are excluded from results.
        /// </summary>
        public string UserId { get; set; }
    }
}
