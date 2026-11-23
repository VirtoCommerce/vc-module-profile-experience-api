using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.CustomerModule.Core.Extensions;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Organization;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Vendor;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;
using VirtoCommerce.ProfileExperienceApiModule.Data.Queries;
using VirtoCommerce.ProfileExperienceApiModule.Data.Queries.AddressesQuery;
using CustomerModuleConstants = VirtoCommerce.CustomerModule.Core.ModuleConstants;
using ProfilePermissions = VirtoCommerce.ProfileExperienceApiModule.Data.ModuleConstants.Security.Permissions;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Authorization
{
    public class ProfileAuthorizationHandler : AuthorizationHandler<ProfileAuthorizationRequirement>
    {
        private readonly IMemberService _memberService;
        private readonly IOrganizationMembershipSearchService _organizationMembershipSearchService;
        private readonly Func<UserManager<ApplicationUser>> _userManagerFactory;


        public ProfileAuthorizationHandler(
            IMemberService memberService,
            IOrganizationMembershipSearchService organizationMembershipSearchService,
            Func<UserManager<ApplicationUser>> userManagerFactory)
        {
            _memberService = memberService;
            _organizationMembershipSearchService = organizationMembershipSearchService;
            _userManagerFactory = userManagerFactory;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ProfileAuthorizationRequirement requirement)
        {
            var result = context.User.IsInRole(PlatformConstants.Security.SystemRoles.Administrator);

            // VCST-6974: the storefront API is a client API and never writes security data (roles, user type,
            // member, external logins) or organization/group membership, regardless of the caller's platform
            // permissions. Those are back-office operations and go through the platform security / customer API.
            // createUser, createContact and updateContact therefore always persist profile fields only.

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

            // VCST-6974: the storefront API is a client API. It operates on the caller's own account and
            // on the caller's CURRENT organization only (the organization_id claim), never on any other
            // organization the caller may also belong to. An empty current organization denies org-scoped operations.
            var currentOrganizationId = context.User.GetCurrentOrganizationId();

            // Whether the caller may manage other members of the current organization (edit, invite, remove, lock, roles).
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
                    result = await HasActiveAccessToOrganizationAsync(currentContact, currentUserId, organizationAggregate.Organization.Id);
                    break;
                case VendorAggregate:
                    result = true;
                    break;
                case Role:
                    //Can be checked only with platform permission
                    result = true;
                    break;
                case CreateContactCommand:
                    //Anonymous user can create contact (self-registration). Membership fields are stripped unless CanChangeMembership.
                    result = true;
                    break;
                case CreateOrganizationCommand:
                    //New user can create organization on b2b-theme (company registration)
                    result = true;
                    break;
                case CreateUserCommand createUserCommand:
                    //Anonymous user can create customer users only (self-registration). Security fields are stripped unless CanSetSecurityData.
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
                case CurrentCustomerAddressesQuery when currentMember != null:
                case CurrentOrganizationAddressesQuery when currentMember != null:
                    result = true;
                    break;
                case CheckDuplicateAddressQuery duplicateAddressQuery:
                    result = await IsSelfOrInCurrentOrganizationAsync(duplicateAddressQuery.MemberId, currentOrganizationId, userManager, currentMember, currentContact);
                    break;
                case MemberCommand memberCommand:
                    result = await IsSelfOrInCurrentOrganizationAsync(memberCommand.MemberId, currentOrganizationId, userManager, currentMember, currentContact);
                    break;
                case UpdateContactCommand updateContactCommand when currentContact != null:
                    // Own contact: always. Another member of the current organization: only with the org-edit permission (VCST-6974 #5).
                    result = updateContactCommand.Id == currentContact.Id;
                    result = result || (canManageOrganization && await IsInCurrentOrganizationAsync(currentContact, currentOrganizationId, updateContactCommand.Id, userManager));
                    break;
                case UpdateOrganizationCommand updateOrganizationCommand when currentContact != null:
                    // Only the caller's current organization, and only with the org-edit permission.
                    result = canManageOrganization
                        && !string.IsNullOrEmpty(currentOrganizationId)
                        && updateOrganizationCommand.Id.EqualsIgnoreCase(currentOrganizationId)
                        && currentContact.Organizations.Contains(currentOrganizationId);
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
                    // VCST-6974: the storefront API updates the caller's OWN account only. Managing other users
                    // or security data is a back-office operation (platform security API).
                    result = !string.IsNullOrEmpty(currentUserId) && updateUserCommand.ApplicationUser?.Id == currentUserId;
                    break;
                case UpdatePersonalDataCommand updatePersonalDataCommand:
                    updatePersonalDataCommand.UserId = currentUserId;
                    result = !string.IsNullOrEmpty(currentUserId);
                    break;
                case InviteUserCommand inviteUserCommand:
                    // VCST-6974 #6: invitations are scoped to the caller's current organization only.
                    // An empty organization id no longer bypasses the check. The invite permission itself
                    // (MyOrganizationUserInvite) is enforced by the resolver via CheckAuthAsync.
                    if (currentContact != null && !string.IsNullOrEmpty(currentOrganizationId))
                    {
                        var currentUser = await userManager.FindByIdAsync(currentUserId);
                        result = currentUser != null
                            && inviteUserCommand.OrganizationId.EqualsIgnoreCase(currentOrganizationId)
                            && currentContact.Organizations.Contains(currentOrganizationId)
                            && currentUser.StoreId.EqualsIgnoreCase(inviteUserCommand.StoreId);
                    }
                    break;
                case LockOrganizationContactCommand lockOrganizationContact:
                    result = canManageOrganization && await IsInCurrentOrganizationAsync(currentContact, currentOrganizationId, lockOrganizationContact.MemberId, userManager);
                    break;
                case UnlockOrganizationContactCommand unlockOrganizationContact:
                    result = canManageOrganization && await IsInCurrentOrganizationAsync(currentContact, currentOrganizationId, unlockOrganizationContact.MemberId, userManager);
                    break;
                case ChangeOrganizationContactRoleCommand changeOrganizationContactRoleCommand:
                    result = canManageOrganization && await IsInCurrentOrganizationAsync(currentContact, currentOrganizationId, changeOrganizationContactRoleCommand.MemberId, userManager);
                    break;
                case RemoveMemberFromOrganizationCommand removeMemberFromOrganizationCommand:
                    // VCST-6974 #10: the organization is the caller's current organization (stamped by the resolver), not caller input.
                    result = canManageOrganization
                        && !string.IsNullOrEmpty(currentOrganizationId)
                        && removeMemberFromOrganizationCommand.OrganizationId.EqualsIgnoreCase(currentOrganizationId)
                        && await IsInCurrentOrganizationAsync(currentContact, currentOrganizationId, removeMemberFromOrganizationCommand.ContactId, userManager);
                    break;
                case AcceptOrganizationInviteCommand acceptOrganizationInviteCommand:
                    result = acceptOrganizationInviteCommand.UserId == currentUserId;
                    break;
                case RejectOrganizationInviteCommand rejectOrganizationInviteCommand:
                    result = rejectOrganizationInviteCommand.UserId == currentUserId;
                    break;
                case RevokeOrganizationInviteCommand revokeOrganizationInviteCommand:
                    result = canManageOrganization && await IsInCurrentOrganizationAsync(currentContact, currentOrganizationId, revokeOrganizationInviteCommand.MemberId, userManager);
                    break;
                case ResendOrganizationInviteCommand resendOrganizationInviteCommand:
                    result = canManageOrganization && await IsInCurrentOrganizationAsync(currentContact, currentOrganizationId, resendOrganizationInviteCommand.MemberId, userManager);
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
                var member = await _memberService.GetByIdAsync(memberId, MemberResponseGroup.Default.ToString());
                if (member == null)
                {
                    return false;
                }

                if (member.MemberType.EqualsIgnoreCase("Organization"))
                {
                    // The member is an organization: it must be the caller's current organization.
                    result = !string.IsNullOrEmpty(currentOrganizationId)
                        && member.Id.EqualsIgnoreCase(currentOrganizationId)
                        && currentContact.Organizations.Any(x => x.EqualsIgnoreCase(currentOrganizationId));
                }
                else
                {
                    result = await IsInCurrentOrganizationAsync(currentContact, currentOrganizationId, memberId, userManager);
                }
            }

            return result;
        }

        private async Task<bool> HasActiveAccessToOrganizationAsync(Contact currentContact, string userId, string organizationId)
        {
            if (!currentContact.Organizations.Contains(organizationId))
            {
                return false;
            }

            var membership = await _organizationMembershipSearchService.GetMembershipAsync(userId, organizationId);
            if (membership?.IsCurrentlyLocked == true)
            {
                return false;
            }

            var effectiveStatus = OrganizationMembership.ResolveEffectiveStatus(membership?.Status, currentContact.Status);

            return !CustomerModuleConstants.MembershipStatuses.IsBlocking(effectiveStatus);
        }

        /// <summary>
        /// VCST-6974: the caller and the target contact must both belong to the caller's CURRENT organization
        /// (the organization_id claim). A caller that merely shares some other organization with the target is not enough.
        /// </summary>
        private async Task<bool> IsInCurrentOrganizationAsync(Contact currentContact, string currentOrganizationId, string contactId, UserManager<ApplicationUser> userManager)
        {
            if (currentContact is null || string.IsNullOrEmpty(currentOrganizationId))
            {
                return false;
            }

            // The caller must actually be a member of the organization they are acting in.
            if (!currentContact.Organizations.Any(x => x.EqualsIgnoreCase(currentOrganizationId)))
            {
                return false;
            }

            var contact = await GetCustomerAsync(contactId, userManager) as Contact;
            return contact?.Organizations?.Any(x => x.EqualsIgnoreCase(currentOrganizationId)) == true;
        }

        protected virtual async Task<Member> GetCustomerAsync(string customerId, UserManager<ApplicationUser> userManager)
        {
            if (string.IsNullOrWhiteSpace(customerId))
            {
                return null;
            }

            var result = await _memberService.GetByIdAsync(customerId, MemberResponseGroup.Default.ToString());

            if (result == null)
            {
                var user = await userManager.FindByIdAsync(customerId);

                if (user?.MemberId != null)
                {
                    result = await _memberService.GetByIdAsync(user.MemberId, MemberResponseGroup.Default.ToString());
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

            // reload contact with addresses
            contact = await _memberService.GetByIdAsync(contact.Id, MemberResponseGroup.WithAddresses.ToString()) as Contact;

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

            var organizations = await _memberService.GetByIdsAsync(contact.Organizations.ToArray(), MemberResponseGroup.WithAddresses.ToString());

            return organizations
                .SelectMany(x => x.Addresses)
                .Any(x => x.Key == addressId);
        }
    }
}
