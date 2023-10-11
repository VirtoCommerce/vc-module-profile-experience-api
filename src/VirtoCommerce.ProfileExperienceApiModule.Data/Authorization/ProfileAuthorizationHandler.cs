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

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Authorization
{
    public class ProfileAuthorizationHandler : AuthorizationHandler<ProfileAuthorizationRequirement>
    {
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

            var currentUserId = GetUserId(context);
            var currentMember = await GetCustomerAsync(currentUserId, userManager);
            var currentContact = currentMember as Contact;

            // PT-6083: reduce complexity
            if (context.Resource is ContactAggregate contactAggregate && currentContact != null)
            {
                result = currentContact.Id == contactAggregate.Contact.Id;
                if (!result)
                {
                    result = await HasSameOrganizationAsync(currentContact, contactAggregate.Contact.Id, userManager);
                }
            }
            else if (context.Resource is ApplicationUser applicationUser)
            {
                result = currentUserId == applicationUser.Id;
                if (!result)
                {
                    result = await HasSameOrganizationAsync(currentContact, applicationUser.Id, userManager);
                }
            }
            else if (context.Resource is OrganizationAggregate organizationAggregate && currentContact != null)
            {
                result = currentContact.Organizations.Contains(organizationAggregate.Organization.Id);
            }
            else if (context.Resource is VendorAggregate)
            {
                result = true;
            }

            else if (context.Resource is Role role)
            {
                //Can be checked only with platform permission
                result = true;
            }
            else if (context.Resource is CreateContactCommand createContactCommand)
            {
                //Anonymous user can create contact
                result = true;
            }
            else if (context.Resource is CreateOrganizationCommand createOrganizationCommand)
            {
                //New user can create organization on b2b-theme
                result = true;
            }
            else if (context.Resource is CreateUserCommand createUserCommand)
            {
                //Anonymous user can create customer users only
                result = !createUserCommand.ApplicationUser.IsAdministrator && createUserCommand.ApplicationUser.UserType.EqualsInvariant("Customer");
            }
            else if (context.Resource is SendVerifyEmailCommand)
            {
                //Anonymous user request verification email
                result = true;
            }
            else if (context.Resource is DeleteContactCommand deleteContactCommand && currentContact != null)
            {
                result = await HasSameOrganizationAsync(currentContact, deleteContactCommand.ContactId, userManager);
            }
            else if (context.Resource is DeleteUserCommand deleteUserCommand && currentContact != null)
            {
                var allowDelete = true;
                foreach (var userName in deleteUserCommand.UserNames)
                {
                    if (allowDelete)
                    {
                        var user = await userManager.FindByNameAsync(userName);
                        allowDelete = await HasSameOrganizationAsync(currentContact, user.MemberId, userManager);
                    }
                }

                result = allowDelete;
            }
            else if (context.Resource is MemberCommand memberCommand)
            {
                result = memberCommand.MemberId == currentMember?.Id;
                if (!result && currentContact != null)
                {
                    var memberId = memberCommand.MemberId;
                    var member = await _memberService.GetByIdAsync(memberId);

                    if (member.MemberType.EqualsInvariant("Organization") && currentContact.Organizations.Any(x => x.EqualsInvariant(member.Id)))
                    {
                        result = true;
                    }
                    else
                    {
                        result = await HasSameOrganizationAsync(currentContact, memberId, userManager);
                    }
                }
            }
            else if (context.Resource is UpdateContactCommand updateContactCommand && currentContact != null)
            {
                result = updateContactCommand.Id == currentContact.Id;
                if (!result)
                {
                    result = await HasSameOrganizationAsync(currentContact, updateContactCommand.Id, userManager);
                }
            }
            else if (context.Resource is UpdateOrganizationCommand updateOrganizationCommand && currentContact != null)
            {
                result = currentContact.Organizations.Contains(updateOrganizationCommand.Id);
            }
            else if (context.Resource is UpdateMemberDynamicPropertiesCommand)
            {
                //Can be checked only with platform permission
                result = true;
            }
            else if (context.Resource is UpdateRoleCommand updateRoleCommand)
            {
                //Can be checked only with platform permission
                result = true;
            }
            else if (context.Resource is UpdateUserCommand updateUserCommand && currentContact != null)
            {
                result = updateUserCommand.ApplicationUser.Id == currentContact.Id;
                if (!result)
                {
                    result = await HasSameOrganizationAsync(currentContact, updateUserCommand.ApplicationUser.Id, userManager);
                }
            }
            else if (context.Resource is UpdatePersonalDataCommand updatePersonalDataCommand)
            {
                updatePersonalDataCommand.UserId = currentUserId;
                result = true;
            }
            else if (context.Resource is InviteUserCommand inviteUserCommand)
            {
                if (!string.IsNullOrEmpty(inviteUserCommand.OrganizationId) && currentContact != null)
                {
                    var currentUser = await userManager.FindByIdAsync(currentUserId);
                    result = currentContact.Organizations.Contains(inviteUserCommand.OrganizationId) && currentUser.StoreId.EqualsInvariant(inviteUserCommand.StoreId);
                }
                else
                {
                    result = true;
                }
            }
            else if (context.Resource is LockOrganizationContactCommand lockOrganizationContact)
            {
                result = await HasSameOrganizationAsync(currentContact, lockOrganizationContact.UserId, userManager);
            }
            else if (context.Resource is UnlockOrganizationContactCommand unlockOrganizationContact)
            {
                result = await HasSameOrganizationAsync(currentContact, unlockOrganizationContact.UserId, userManager);
            }
            else if (context.Resource is ChangeOrganizationContactRoleCommand changeOrganizationContactRoleCommand)
            {
                result = await HasSameOrganizationAsync(currentContact, changeOrganizationContactRoleCommand.UserId, userManager);
            }
            else if (context.Resource is RemoveMemberFromOrganizationCommand removeMemberFromOrganizationCommand)
            {
                result = await HasSameOrganizationAsync(currentContact, removeMemberFromOrganizationCommand.ContactId, userManager);
            }
            else if (context.Resource is ChangePasswordCommand changePasswordCommand)
            {
                result = changePasswordCommand.UserId == currentUserId;
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

        private static string GetUserId(AuthorizationHandlerContext context)
        {
            //PT-5375 use ClaimTypes instead of "name"
            return context.User.FindFirstValue("name");
        }

        private async Task<bool> HasSameOrganizationAsync(Contact currentContact, string contactId, UserManager<ApplicationUser> userManager)
        {
            if (currentContact is null)
            {
                return false;
            }

            var contact = await GetCustomerAsync(contactId, userManager) as Contact;
            return currentContact.Organizations.Intersect(contact?.Organizations ?? Array.Empty<string>()).Any();
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
    }
}
