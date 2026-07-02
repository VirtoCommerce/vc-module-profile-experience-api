using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Model.Search;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class SearchMembersQueryHandler :
        IRequestHandler<SearchContactsQuery, MemberSearchResult>,
        IRequestHandler<SearchOrganizationsQuery, MemberSearchResult>
    {
        private readonly IMemberSearchService _memberSearchService;
        private readonly IOrganizationMembershipSearchService _organizationMembershipSearchService;

        public SearchMembersQueryHandler(
            IMemberSearchService memberSearchService,
            IOrganizationMembershipSearchService organizationMembershipSearchService)
        {
            _memberSearchService = memberSearchService;
            _organizationMembershipSearchService = organizationMembershipSearchService;
        }

        public virtual Task<MemberSearchResult> Handle(SearchContactsQuery request, CancellationToken cancellationToken)
        {
            var searchCriteria = BuildMembersSearchCriteria(request, nameof(Contact));

            return _memberSearchService.SearchMembersAsync(searchCriteria);
        }

        public virtual async Task<MemberSearchResult> Handle(SearchOrganizationsQuery request, CancellationToken cancellationToken)
        {
            var searchCriteria = BuildMembersSearchCriteria(request, nameof(Organization));

            // Exclude locked organizations before pagination, so page sizes and TotalCount stay consistent
            if (!string.IsNullOrEmpty(request.UserId))
            {
                var lockedOrgIds = await _organizationMembershipSearchService.GetLockedOrganizationIdsAsync(request.UserId);
                if (lockedOrgIds.Count > 0)
                {
                    searchCriteria.ExcludedObjectIds = lockedOrgIds.ToArray();
                }
            }

            return await _memberSearchService.SearchMembersAsync(searchCriteria);
        }

        protected virtual MembersSearchCriteria BuildMembersSearchCriteria(SearchMembersQueryBase request, string memberType)
        {
            var result = AbstractTypeFactory<MembersSearchCriteria>.TryCreateInstance();
            result.DeepSearch = request.DeepSearch;
            result.MemberType = memberType;
            result.Keyword = request.Keyword;
            result.Skip = request.Skip;
            result.Take = request.Take;
            result.Sort = request.Sort;
            result.ObjectIds = request.ObjectIds;
            result.MemberId = request.MemberId;

            return result;
        }

    }
}
