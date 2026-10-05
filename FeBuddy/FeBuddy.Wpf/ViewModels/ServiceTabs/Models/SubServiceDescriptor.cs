namespace FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

/// <summary>
/// One entry in a service's sub-service catalogue: what it is called, where it sits in the rail,
/// whether its backend exists yet, what it can make, and how to build its tab.
/// </summary>
/// <remarks>
/// A catalogue of these is the whole registration surface for a sub-service. Adding the next one
/// - AIRAC Service is expected to reach roughly twenty - is one entry here plus its tab
/// view-model; nothing in the shell, the rail, the save contract or the Preview Settings and
/// Review tabs changes.
/// </remarks>
/// <param name="Key">
/// The stable identifier persisted in <c>UserConfig</c> (e.g. <c>Airways</c>). Never localise or
/// rename this without migrating the saved selection and outputs.
/// </param>
/// <param name="DisplayName">The name shown in the General tab's table and on the tab.</param>
/// <param name="Order">Sort order in the General tab's table and the tab rail; lower comes first.</param>
/// <param name="IsImplemented">
/// <see langword="false"/> while the sub-service has no backend. Its tab still opens (so the
/// layout is exercised and the user can see it is coming), but it contributes nothing to a run.
/// </param>
/// <param name="CreateTab">Builds the tab view-model. Called once, when the screen is built.</param>
/// <param name="AliasFileName">
/// The alias file the sub-service can write (e.g. <c>Airways.txt</c>), or <see langword="null"/> when it
/// writes none. The Concatenate Aliases tab lists these, to show which go into <c>Combined_Alias.txt</c>.
/// </param>
/// <param name="Outputs">
/// What the sub-service can make: its columns on the General tab. <see cref="SubServiceOutputKinds.None"/>
/// keeps it off the General tab's table altogether (Concatenate Aliases).
/// </param>
/// <param name="Help">What it and each of its outputs are, for the tooltips; <see langword="null"/> for none.</param>
public sealed record SubServiceDescriptor(
	string Key,
	string DisplayName,
	int Order,
	bool IsImplemented,
	Func<ServiceTabViewModel> CreateTab,
	string? AliasFileName = null,
	SubServiceOutputKinds Outputs = SubServiceOutputKinds.None,
	SubServiceHelp? Help = null);
