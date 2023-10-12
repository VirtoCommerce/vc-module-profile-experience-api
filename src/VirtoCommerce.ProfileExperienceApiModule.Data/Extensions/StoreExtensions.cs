using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.StoreModule.Core.Model;
using StoreSettings = VirtoCommerce.StoreModule.Core.ModuleConstants.Settings.General;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Extensions
{
    public static class StoreExtensions
    {
        public static string GetEmailVerificationFlow(this Store store)
        {
            var emailVerificationEnabled = store.Settings.GetValue<bool>(StoreSettings.EmailVerificationEnabled);
            var emailVerificationRequired = store.Settings.GetValue<bool>(StoreSettings.EmailVerificationRequired);

            if (!emailVerificationEnabled)
            {
                return ModuleConstants.RegistrationFlows.NoEmailVerification;
            }

            if (emailVerificationRequired)
            {
                return ModuleConstants.RegistrationFlows.EmailVerificationRequired;
            }

            return ModuleConstants.RegistrationFlows.EmailVerificationOptional;
        }
    }
}
