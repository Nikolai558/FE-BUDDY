namespace FeBuddy.Wpf.Infrastructure;

/// <summary>
/// One entry in a service's sub-service catalogue: what it is called, where it sits in the rail,
/// whether its backend exists yet, and how to build its tab.
/// </summary>
/// <remarks>
/// A catalogue of these is the whole registration surface for a sub-service. Adding the next one
/// - AIRAC Service is expected to reach roughly twenty - is one entry here plus its tab
/// view-model; nothing in the shell, the rail, the save contract or the Review tab changes.
/// </remarks>
/// <param name="Key">
/// The stable identifier persisted in <c>UserConfig</c> (e.g. <c>Airways</c>). Never localise or
/// rename this without migrating the saved selection.
/// </param>
/// <param name="DisplayName">The name shown in the picker and on the tab.</param>
/// <param name="Order">Sort order in the picker and the tab rail; lower comes first.</param>
/// <param name="IsImplemented">
/// <see langword="false"/> while the sub-service has no backend. Its tab still opens (so the
/// layout is exercised and the user can see it is coming), but it contributes nothing to a run.
/// </param>
/// <param name="CreateTab">Builds the tab view-model. Called once, the first time it is selected.</param>
public sealed record SubServiceDescriptor(
    string Key,
    string DisplayName,
    int Order,
    bool IsImplemented,
    Func<ServiceTabViewModel> CreateTab);
