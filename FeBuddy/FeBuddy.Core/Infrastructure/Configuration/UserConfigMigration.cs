namespace FeBuddy.Core.Infrastructure.Configuration;

/// <summary>
/// One change to the <c>UserConfig</c> layout: what turns a file saved in layout
/// <c><paramref name="ToVersion"/> - 1</c> into layout <paramref name="ToVersion"/>. Listed in
/// <see cref="UserConfigMigrations.All"/>.
/// </summary>
/// <remarks>
/// A step works on the settings by dotted path, through <see cref="UserConfigMigrationContext"/>'s
/// edits, e.g.
/// <code>
/// new UserConfigMigration(2, "Airways' buffer distances moved under Buffer", settings =>
/// {
///     settings.Move("Services.AiracService.Airways.FixBufferNm", "Services.AiracService.Airways.Buffer.FixNm");
///     settings.ChangeValue("Services.AiracService.Airways.Buffer.Enabled", value => value == "true" ? "Y" : "N");
/// })
/// </code>
/// Write a step so it does no harm run twice: <see cref="UserConfigMigrationContext"/>'s edits do
/// nothing when the old setting is already gone, and never overwrite one already in the new place.
/// <see cref="Apply"/> runs on each profile, then on <c>Shared.json</c>; a setting that moves between the
/// two uses <see cref="UserConfigMigrationContext.MoveToShared"/> or
/// <see cref="UserConfigMigrationContext.CopyFromShared"/> and, for the second, <see cref="ThenShared"/>.
/// </remarks>
/// <param name="ToVersion">The layout the step produces.</param>
/// <param name="Description">What changed, for the log.</param>
/// <param name="Apply">Makes the change, on each profile and on <c>Shared.json</c>.</param>
public sealed record UserConfigMigration(int ToVersion, string Description, Action<UserConfigMigrationContext> Apply)
{
	/// <summary>
	/// What the step does to <c>Shared.json</c> once <see cref="Apply"/> has run on every profile and on it:
	/// taking out a setting the profiles have each copied (<see cref="UserConfigMigrationContext.CopyFromShared"/>),
	/// so it is a profile's own from now on. <see langword="null"/> for a step with nothing to do there.
	/// </summary>
	public Action<UserConfigMigrationContext>? ThenShared { get; init; }
}
