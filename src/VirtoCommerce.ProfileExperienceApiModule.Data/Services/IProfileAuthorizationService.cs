using System.Threading.Tasks;
using GraphQL;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Services;

public interface IProfileAuthorizationService
{
    Task CheckAuthAsync(IResolveFieldContext context, object resource, string permission = null, bool checkPasswordExpired = true);
}
