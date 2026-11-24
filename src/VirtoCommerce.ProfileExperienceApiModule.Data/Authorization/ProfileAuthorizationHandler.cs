using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Organization;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Vendor;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;
using ProfilePermissions = VirtoCommerce.ProfileExperienceApiModule.Data.ModuleConstants.Security.Permissions;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Authorization
{
    public class ProfileAuthorizationHandler : AuthorizationHandler<ProfileAuthorizationRequirement>
    {
        // Issued by Customer module 3.811+ (no constant before it); absent on older tokens.
        internal const string OrganizationIdClaimType = "organization_id";

        private readonly IMemberService _memberService;
        private readonly Func<UserManager<ApplicationUser>> _userManagerFactory;


        public ProfileAuthorizationHandler(IMemberService memberService, Func<UserManager<ApplicationUser>> userManagerFactory)
        {
            _memberService = memberService;
            _userManagerFactory = userManagerFactory;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ProfileAuthorizationRequirement requirement)
        {
            var result = context.User.IsInRole(PlatformConstants.Security.SystemRoles.Administrator);

            // Administrators can do anything except creating any users
            if (result && context.Resource is not CreateUserCommand)
            {
                context.Succeed(requirement);
                return;
            }

            using var userManager = _userManagerFactory();

            var currentUserId = GetCurrentUserId(context);
            var currentMember = await GetCustomerAsync(currentUserId, userManager);
            var currentContact = currentMember as Contact;
            var currentOrganizationId = context.User?.FindFirstValue(OrganizationIdClaimType);
            var canManageOrganization = context.User.HasGlobalPermission(ProfilePermissions.MyOrganizationEdit);

            // PT-6083: reduce complexity
            switch (context.Resource)
            {
                case ContactAggregate contactAggregate when currentContact != null:
                    result = currentContact.Id == contactAggregate.Contact.Id;
                    result = result || await IsInCurrentOrganizationAsync(currentContact, currentOrganizationId, contactAggregate.Contact.Id, userManager);
                    break;
                case ApplicationUser applicationUser:
                    result = currentUserId == applicationUser.Id;
                    result = result || await IsInCurrentOrganizationAsync(currentContact, currentOrganizationId, applicationUser.Id, userManager);
                    break;
                case OrganizationAggregate organizationAggregate when currentContact != null:
                    result = currentContact.Organizations.Contains(organizationAggregate.Organization.Id);
                    break;
                case VendorAggregate:
                    result = true;
                    break;
                case Role:
                    //Can be checked only with platform permission
                    result = true;
                    break;
                case CreateContactCommand:
                    //Anonymous user can create contact (self-registration). Organizations and groups are always stripped by the handler.
                    result = true;
                    break;
                case CreateOrganizationCommand:
                    //New user can create organization on b2b-theme (company registration)
                    result = true;
                    break;
                case CreateUserCommand createUserCommand:
                    //Anonymous user can create customer users only (self-registration). Security fields are always stripped by the handler.
                    result = !createUserCommand.ApplicationUser.IsAdministrator && createUserCommand.ApplicationUser.UserType.EqualsIgnoreCase("Customer");
                    break;
                case SendVerifyEmailCommand:
                    //Anonymous user request verification email
                    result = true;
                    break;
                case DeleteContactCommand deleteContactCommand when currentContact != null:
                    result = await IsInCurrentOrganizationAsync(currentContact, currentOrganizationId, deleteContactCommand.ContactId, userManager);
                    break;
                case DeleteUserCommand deleteUserCommand when currentContact != null:
                    var allowDelete = true;
                    foreach (var userName in deleteUserCommand.UserNames)
                    {
                        if (allowDelete)
                        {
                            var user = await userManager.FindByNameAsync(userName);
                            allowDelete = await IsInCurrentOrganizationAsync(currentContact, currentOrganizationId, user?.MemberId, userManager);
                        }
                    }
                    result = allowDelete;
                    break;
                case MemberCommand memberCommand:
                    result = await IsSelfOrInCurrentOrganizationAsync(memberCommand.MemberId, currentOrganizationId, userManager, currentMember, currentContact);
                    break;
                case UpdateContactCommand updateContactCommand when currentContact != null:
                    result = updateContactCommand.Id == currentContact.Id;
                    result = result || (canManageOrganization && await IsInCurrentOrganizationAsync(currentContact, currentOrganizationId, updateContactCommand.Id, userManager));
                    break;
                case UpdateOrganizationCommand updateOrganizationCommand when currentContact != null:
                    result = canManageOrganization && IsCurrentOrganization(currentContact, currentOrganizationId, updateOrganizationCommand.Id);
                    break;
                case UpdateMemberDynamicPropertiesCommand:
                    //Can be checked only with platform permission
                    result = true;
                    break;
                case UpdateRoleCommand:
                    //Can be checked only with platform permission
                    result = true;
                    break;
                case UpdateUserCommand updateUserCommand:
                    result = !string.IsNullOrEmpty(currentUserId) && updateUserCommand.ApplicationUser?.Id == currentUserId;
                    break;
                case UpdatePersonalDataCommand updatePersonalDataCommand:
                    updatePersonalDataCommand.UserId = currentUserId;
                    result = !string.IsNullOrEmpty(currentUserId);
                    break;
                case InviteUserCommand inviteUserCommand:
                    if (currentContact != null && !string.IsNullOrEmpty(inviteUserCommand.OrganizationId))
                    {
                        var currentUser = await userManager.FindByIdAsync(currentUserId);
                        result = currentUser != null
                            && currentContact.Organizations.Any(x => x.EqualsIgnoreCase(inviteUserCommand.OrganizationId))
                            && currentUser.StoreId.EqualsIgnoreCase(inviteUserCommand.StoreId);
                    }
                    break;
                case LockOrganizationContactCommand lockOrganizationContact:
                    result = canManageOrganization && await IsInCurrentOrganizationAsync(currentContact, currentOrganizationId, lockOrganizationContact.UserId, userManager);
                    break;
                case UnlockOrganizationContactCommand unlockOrganizationContact:
                    result = canManageOrganization && await IsInCurrentOrganizationAsync(currentContact, currentOrganizationId, unlockOrganizationContact.UserId, userManager);
                    break;
                case ChangeOrganizationContactRoleCommand changeOrganizationContactRoleCommand:
                    result = canManageOrganization && await IsInCurrentOrganizationAsync(currentContact, currentOrganizationId, changeOrganizationContactRoleCommand.UserId, userManager);
                    break;
                case RemoveMemberFromOrganizationCommand removeMemberFromOrganizationCommand:
                    result = await IsInOrganizationAsync(currentContact, removeMemberFromOrganizationCommand.OrganizationId, removeMemberFromOrganizationCommand.ContactId, userManager);
                    break;
                case ChangePasswordCommand changePasswordCommand:
                    result = changePasswordCommand.UserId == currentUserId;
                    break;
                case AddAddressToFavoritesCommand command:
                    result = await CanAccessAddressAsync(currentContact, command.AddressId);
                    break;
                case RemoveAddressFromFavoritesCommand:
                    // Can remove any address from favorites if user is not anonymous
                    result = currentContact != null;
                    break;
            }

            if (result)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
        }

        private static string GetCurrentUserId(AuthorizationHandlerContext context)
        {
            return context.User.GetUserId();
        }

        private async Task<bool> IsSelfOrInCurrentOrganizationAsync(string memberId, string currentOrganizationId, UserManager<ApplicationUser> userManager, Member currentMember, Contact currentContact)
        {
            var result = memberId == currentMember?.Id;
            if (!result && currentContact != null)
            {
                var member = await _memberService.GetByIdAsync(memberId);
                if (member == null)
                {
                    return false;
                }

                if (member.MemberType.EqualsIgnoreCase("Organization"))
                {
                    // The member is an organization: it must be the caller's current organization.
                    result = IsCurrentOrganization(currentContact, currentOrganizationId, member.Id);
                }
                else
                {
                    result = await IsInCurrentOrganizationAsync(currentContact, currentOrganizationId, memberId, userManager);
                }
            }

            return result;
        }

        // Customer module before 3.811 issues no organization_id claim, so "current organization"
        // falls back to any organization the caller belongs to.
        private static bool IsCurrentOrganization(Contact currentContact, string currentOrganizationId, string organizationId)
        {
            if (currentContact?.Organizations is null || string.IsNullOrEmpty(organizationId))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(currentOrganizationId) && !organizationId.EqualsIgnoreCase(currentOrganizationId))
            {
                return false;
            }

            return currentContact.Organizations.Any(x => x.EqualsIgnoreCase(organizationId));
        }

        private async Task<bool> IsInCurrentOrganizationAsync(Contact currentContact, string currentOrganizationId, string contactId, UserManager<ApplicationUser> userManager)
        {
            if (!string.IsNullOrEmpty(currentOrganizationId))
            {
                return await IsInOrganizationAsync(currentContact, currentOrganizationId, contactId, userManager);
            }

            if (currentContact?.Organizations is null)
            {
                return false;
            }

            var contact = await GetCustomerAsync(contactId, userManager) as Contact;
            return currentContact.Organizations.Any(x => contact?.Organizations?.Any(y => y.EqualsIgnoreCase(x)) == true);
        }

        /// <summary>Whether both the caller and the target contact are members of the organization.</summary>
        private async Task<bool> IsInOrganizationAsync(Contact currentContact, string organizationId, string contactId, UserManager<ApplicationUser> userManager)
        {
            if (currentContact is null || string.IsNullOrEmpty(organizationId))
            {
                return false;
            }

            if (!currentContact.Organizations.Any(x => x.EqualsIgnoreCase(organizationId)))
            {
                return false;
            }

            var contact = await GetCustomerAsync(contactId, userManager) as Contact;
            return contact?.Organizations?.Any(x => x.EqualsIgnoreCase(organizationId)) == true;
        }

        protected virtual async Task<Member> GetCustomerAsync(string customerId, UserManager<ApplicationUser> userManager)
        {
            if (string.IsNullOrWhiteSpace(customerId))
            {
                return null;
            }

            var result = await _memberService.GetByIdAsync(customerId);

            if (result == null)
            {
                var user = await userManager.FindByIdAsync(customerId);

                if (user?.MemberId != null)
                {
                    result = await _memberService.GetByIdAsync(user.MemberId);
                }
            }

            return result;
        }

        protected virtual async Task<bool> CanAccessAddressAsync(Contact contact, string addressId)
        {
            if (contact is null)
            {
                return false;
            }

            if (contact.Addresses != null && contact.Addresses.Any(x => x.Key == addressId))
            {
                return true;
            }

            if (contact.Organizations.IsNullOrEmpty())
            {
                return false;
            }

            var organizations = await _memberService.GetByIdsAsync(contact.Organizations.ToArray());

            return organizations
                .SelectMany(x => x.Addresses)
                .Any(x => x.Key == addressId);
        }
    }
}
