using System;
using FeBuddy.Versioning;
using WixToolset.Dtf.WindowsInstaller;

namespace FeBuddy.Installer.CustomActions
{
    public static class VersionPolicyActions
    {
        /// <summary>
        /// Runs early in both the UI and Execute sequences (scheduled After="AppSearch" in
        /// Package.wxs, so INSTALLEDPRODUCTSEMVER - populated by a RegistrySearch - is already
        /// available). Decides whether this install/upgrade/downgrade is allowed under
        /// FE-BUDDY's version policy (see FeBuddy.Versioning.UpdatePolicy) and records the
        /// verdict in the VERSIONPOLICY_BLOCKED property, which a LaunchCondition in
        /// Package.wxs then acts on.
        ///
        /// This CA never fails the install itself (always returns Success) - it only ever
        /// sets VERSIONPOLICY_BLOCKED. The LaunchCondition is what actually stops setup,
        /// so the user sees FE-BUDDY's own localized error dialog instead of a raw CA failure.
        /// </summary>
        [CustomAction]
        public static ActionResult EnforceVersionPolicy(Session session)
        {
            string installedVersionText = session["INSTALLEDPRODUCTSEMVER"];
            string candidateVersionText = session["PRODUCTSEMVER"];

            session.Log(
                $"EnforceVersionPolicy: installed='{installedVersionText}' candidate='{candidateVersionText}'");

            bool allowed;
            try
            {
                allowed = UpdatePolicy.IsTransitionAllowed(installedVersionText, candidateVersionText);
            }
            catch (Exception ex)
            {
                // Fail open: a bug in version parsing should never brick an install. Log loudly
                // so it's visible in the MSI log (msiexec /L*v) rather than silently swallowed.
                session.Log($"EnforceVersionPolicy: unexpected error, allowing install. {ex}");
                allowed = true;
            }

            session["VERSIONPOLICY_BLOCKED"] = allowed ? "0" : "1";
            session.Log($"EnforceVersionPolicy: verdict allowed={allowed}");

            return ActionResult.Success;
        }
    }
}
