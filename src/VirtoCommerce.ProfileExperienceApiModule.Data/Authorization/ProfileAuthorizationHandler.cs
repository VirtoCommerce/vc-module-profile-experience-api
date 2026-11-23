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
            var currentOrganizationId = context.User.GetCurrentOrganizationId();
            var canManageOrganization = context.User.HasGlobalPermission(ProfilePermissions.MyOrganizationEdit);

            // PT-6083: reduce complexity
            switch (context.Resource)
            {
                case ContactAggregate contactAggregate when currentContact != null:
                    result = currentContact.Id == contactAggregate.Contact.Id;
                    result = result || await IsInOrganizationAsync(currentContact, currentOrganizationId, contactAggregate.Contact.Id, userManager);
                    break;
                case ApplicationUser applicationUser:
                    result = currentUserId == applicationUser.Id;
                    result = result || await IsInOrganizationAsync(currentContact, currentOrganizationId, applicationUser.Id, userManager);
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
                    result = await IsInOrganizationAsync(currentContact, currentOrganizationId, deleteContactCommand.ContactId, userManager);
                    break;
                case DeleteUserCommand deleteUserCommand when currentContact != null:
                    var allowDelete = true;
                    foreach (var userName in deleteUserCommand.UserNames)
                    {
                        if (allowDelete)
                        {
                            var user = await userManager.FindByNameAsync(userName);
                            allowDelete = await IsInOrganizationAsync(currentContact, currentOrganizationId, user?.MemberId, userManager);
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
                    result = updateContactCommand.Id == currentContact.Id;
                    result = result || (canManageOrganization && await IsInOrganizationAsync(currentContact, currentOrganizationId, updateContactCommand.Id, userManager));
                    break;
                case UpdateOrganizationCommand updateOrganizationCommand when currentContact != null:
                    result = canManageOrganization
                        && !string.IsNullOrEmpty(currentOrganizationId)
                        && updateOrganizationCommand.Id.EqualsIgnoreCase(currentOrganizationId)
                        && currentContact.Organizations.Any(x => x.EqualsIgnoreCase(currentOrganizationId));
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
                    result = canManageOrganization && await IsInOrganizationAsync(currentContact, currentOrganizationId, lockOrganizationContact.MemberId, userManager);
                    break;
                case UnlockOrganizationContactCommand unlockOrganizationContact:
                    result = canManageOrganization && await IsInOrganizationAsync(currentContact, currentOrganizationId, unlockOrganizationContact.MemberId, userManager);
                    break;
                case ChangeOrganizationContactRoleCommand changeOrganizationContactRoleCommand:
                    result = canManageOrganization && await IsInOrganizationAsync(currentContact, currentOrganizationId, changeOrganizationContactRoleCommand.MemberId, userManager);
                    break;
                case RemoveMemberFromOrganizationCommand removeMemberFromOrganizationCommand:
                    result = await IsInOrganizationAsync(currentContact, removeMemberFromOrganizationCommand.OrganizationId, removeMemberFromOrganizationCommand.ContactId, userManager);
                    break;
                case AcceptOrganizationInviteCommand acceptOrganizationInviteCommand:
                    result = acceptOrganizationInviteCommand.UserId == currentUserId;
                    break;
                case RejectOrganizationInviteCommand rejectOrganizationInviteCommand:
                    result = rejectOrganizationInviteCommand.UserId == currentUserId;
                    break;
                case RevokeOrganizationInviteCommand revokeOrganizationInviteCommand:
                    result = canManageOrganization && await IsInOrganizationAsync(currentContact, currentOrganizationId, revokeOrganizationInviteCommand.MemberId, userManager);
                    break;
                case ResendOrganizationInviteCommand resendOrganizationInviteCommand:
                    result = canManageOrganization && await IsInOrganizationAsync(currentContact, currentOrganizationId, resendOrganizationInviteCommand.MemberId, userManager);
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
                    result = await IsInOrganizationAsync(currentContact, currentOrganizationId, memberId, userManager);
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
