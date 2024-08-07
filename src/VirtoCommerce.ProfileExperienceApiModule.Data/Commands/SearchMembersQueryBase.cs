using System.Collections.Generic;
using VirtoCommerce.CustomerModule.Core.Model.Search;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class SearchMembersQueryBase : IQuery<MemberSearchResult>
    {
        public string Keyword { get; set; }
        public string Sort { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }
        public IList<string> ObjectIds { get; set; }
        public bool DeepSearch { get; set; }
        public string MemberId { get; set; }
    }
}
