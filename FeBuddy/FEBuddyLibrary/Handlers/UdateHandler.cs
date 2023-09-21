using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Squirrel;

namespace FEBuddyLibrary.Handlers;

/// <summary>
/// Manages application updates and related events.
/// </summary>
public class UdateHandler
{
  // TODO UdateHandler._gitRepoUrl - Can this URL be moved to a config file/Grabbed from the application settings?
  // TODO UdateHandler._gitRepoUrl - Change the URL to be https://github.com/Nikolai558/FE-BUDDY.
  /// <summary>
  /// GitHub repository URL for the application.
  /// </summary>
  static readonly string _gitRepoUrl = "https://github.com/Nikolai558/FE-Buddy-DEV";

  /// <summary>
  /// Initializes a new instance of the UpdateHandler class.
  /// </summary>
  public UdateHandler()
  {
    // Register event handlers for application installation, update, and uninstallation.
    SquirrelAwareApp.HandleEvents(OnAppInstalled, OnAppUpdated, null, OnAppUninstalled);
  }

  /// <summary>
  /// Handles the event when the application is installed.
  /// </summary>
  /// <param name="version">The version of the application being installed.</param>
  /// <param name="tools">An instance of IAppTools for managing application-related tools.</param>
  private static void OnAppInstalled(SemanticVersion version, IAppTools tools)
  {
    // Create shortcuts for the application on the Start Menu and Desktop.
    tools.CreateShortcutForThisExe(ShortcutLocation.StartMenuRoot | ShortcutLocation.Desktop);
  }

  /// <summary>
  /// Handles the event when the application is updated to a new version.
  /// </summary>
  /// <param name="version">The new version of the application.</param>
  /// <param name="tools">An instance of IAppTools for managing application-related tools.</param>
  private static void OnAppUpdated(SemanticVersion version, IAppTools tools)
  {

  }

  /// <summary>
  /// Handles the event when the application is uninstalled.
  /// </summary>
  /// <param name="version">The version of the application being uninstalled.</param>
  /// <param name="tools">An instance of IAppTools for managing application-related tools.</param>
  private static void OnAppUninstalled(SemanticVersion version, IAppTools tools)
  {
    // Remove shortcuts from the Start Menu and Desktop associated with this application.
    tools.RemoveShortcutForThisExe(ShortcutLocation.StartMenuRoot | ShortcutLocation.Desktop);

    // TODO updateHandler.OnAppUninstalled - Remove all temporary files and folders.
  }

  /// <summary>
  /// Updates the application to the latest version from Github.
  /// </summary>
  /// <exception cref="Exception">Throws an exception if the Installed Update Result is null stating 'Update Failed.'</exception>
  public static void Update()
  {
    // Create an instance of GithubUpdateManager to manage updates for a specific GitHub repository.
    using GithubUpdateManager githubUpdateManager = new GithubUpdateManager(_gitRepoUrl);

    // Attempt to update the application using the GitHub repository.
    ReleaseEntry installedUpdate = githubUpdateManager.UpdateApp().Result;

    if (installedUpdate != null)
    {
      // If an update was successfully installed, restart the application.
      UpdateManager.RestartApp();
    }
    else
    {
      // If the update process failed, throw an exception with an error message.
      throw new Exception("Update failed.");
    }
  }


  /// <summary>
  /// Gets the latest version of the application from Github. 
  /// </summary>
  /// <returns>The latest/current version as a string, or "dev" if the program is not an installed app.</returns>
  public static string GetLatestVersion()
  {
    // Create an instance of Squirrel's GithubUpdateManager to manage updates.
    
    using GithubUpdateManager githubManager = new GithubUpdateManager(_gitRepoUrl);

    // Check if GithubUpdateManager is null or the app is not installed.
    if (githubManager == null || !githubManager.IsInstalledApp) 
    {
      // Return "dev" indicating a development version.
      return "dev"; 
    }

		try
		{
      // Check for updates on the GitHub repository.
      UpdateInfo? updateInfo = githubManager.CheckForUpdate().Result;

			if (updateInfo != null && updateInfo.ReleasesToApply.Count > 0)
			{
        // Return the version of the future release if updates are available.
        return updateInfo.FutureReleaseEntry.Version.ToString();
      }
      else
			{
        // Return the currently installed version or "dev" if not available.
        return updateInfo?.CurrentlyInstalledVersion.Version.ToString() ?? "dev";
      }
		}
		catch (Exception)
		{
      // TODO UdateHandler.GetLatestVersion - Add Error Logging Message here.

      // Re-throw the exception to propagate it up the call stack.
      throw;
		}
  }

}
