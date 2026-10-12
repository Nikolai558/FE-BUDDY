// The compiler looks the type up by this exact name, so the namespace cannot follow the folder.
#pragma warning disable IDE0130
namespace System.Runtime.CompilerServices;
#pragma warning restore IDE0130

/// <summary>
/// Lets records and <c>init</c> accessors compile for net472, whose base library predates them.
/// The compiler only needs the type to exist.
/// </summary>
internal static class IsExternalInit
{
}
