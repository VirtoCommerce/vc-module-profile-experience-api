using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries;

public class GetAssignableCompanyRolesQueryHandler : IQueryHandler<GetAssignableCompanyRolesQuery, IList<Role>>
{
    private readonly ICompanyMemberRoleService _companyMemberRoleService;
    private readonly Func<RoleManager<Role>> _roleManagerFactory;

    public GetAssignableCompanyRolesQueryHandler(
        ICompanyMemberRoleService companyMemberRoleService,
        Func<RoleManager<Role>> roleManagerFactory)
    {
        _companyMemberRoleService = companyMemberRoleService;
        _roleManagerFactory = roleManagerFactory;
    }

    public virtual async Task<IList<Role>> Handle(GetAssignableCompanyRolesQuery request, CancellationToken cancellationToken)
    {
        var allowedRoleIds = await _companyMemberRoleService.GetAllowedRoleIdsAsync(request.StoreId);

        using var roleManager = _roleManagerFactory();
        var roles = roleManager.Roles.ToList();

        return roles.Where(allowedRoleIds.IsRoleAllowed).ToList();
    }
}
