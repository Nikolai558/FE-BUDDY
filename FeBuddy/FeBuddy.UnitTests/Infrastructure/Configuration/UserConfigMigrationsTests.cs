using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Configuration.Models;

namespace FeBuddy.UnitTests.Infrastructure.Configuration;

/// <summary>
/// Covers <see cref="UserConfigMigrations"/> and the edits a step is made of
/// (<see cref="UserConfigMigrationContext"/>): steps run in order from the file's layout to the
/// current one, a missing or failing step stops it with the input untouched, the real steps cover
/// every layout, and every edit can run twice without harm.
/// </summary>
public sealed class UserConfigMigrationsTests
{
	private static readonly UserConfigMigration ToTwo = new(2, "Renamed A to B", settings => settings.Move("General.A", "General.B"));

	private static readonly UserConfigMigration ToThree = new(3, "Y/N for C", settings => settings.ChangeValue("General.C", value => value == "true" ? "Y" : "N"));

	private static Dictionary<string, string> Settings(params (string Key, string Value)[] values) =>
		values.ToDictionary(v => v.Key, v => v.Value, StringComparer.Ordinal);

	private static UserConfigMigrationContext Context(Dictionary<string, string> values) => new(values);

	// ---- the runner ----

	[Fact]
	public void steps_run_in_order_from_the_files_layout_to_the_current_one()
	{
		Dictionary<string, string> values = Settings(("General.A", "1"), ("General.C", "true"));

		UserConfigMigrationResult result = UserConfigMigrations.Migrate(values, 1, 3, [ToThree, ToTwo]);

		Assert.Equal(Settings(("General.B", "1"), ("General.C", "Y")), result.Values);
		Assert.Equal(["Renamed A to B", "Y/N for C"], result.Applied);
		Assert.Equal((1, 3), (result.FromVersion, result.ToVersion));
		Assert.True(result.Migrated);
	}

	[Fact]
	public void a_file_already_in_a_later_layout_skips_the_steps_before_it()
	{
		UserConfigMigrationResult result = UserConfigMigrations.Migrate(Settings(("General.A", "1"), ("General.C", "true")), 2, 3, [ToTwo, ToThree]);

		Assert.Equal(Settings(("General.A", "1"), ("General.C", "Y")), result.Values);
		Assert.Equal(["Y/N for C"], result.Applied);
	}

	[Fact]
	public void a_file_in_the_current_layout_comes_back_as_it_is_and_the_input_is_never_changed()
	{
		Dictionary<string, string> values = Settings(("General.A", "1"));

		UserConfigMigrationResult same = UserConfigMigrations.Migrate(values, 3, 3, [ToTwo, ToThree]);
		UserConfigMigrationResult moved = UserConfigMigrations.Migrate(values, 1, 2, [ToTwo]);

		Assert.Equal(values, same.Values);
		Assert.False(same.Migrated);
		Assert.Equal(Settings(("General.B", "1")), moved.Values);
		Assert.Equal(Settings(("General.A", "1")), values);
	}

	[Fact]
	public void a_missing_step_stops_it()
	{
		InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => UserConfigMigrations.Migrate(Settings(), 1, 3, [ToTwo]));

		Assert.Equal("No step brings settings to layout 3.", ex.Message);
	}

	[Fact]
	public void a_failing_step_stops_it_naming_the_layout_and_what_it_does()
	{
		UserConfigMigration failing = new(2, "Splits the facility list", _ => throw new FormatException("bad list"));

		InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => UserConfigMigrations.Migrate(Settings(), 1, 2, [failing]));

		Assert.Equal("Bringing settings to layout 2 failed (Splits the facility list): bad list", ex.Message);
		Assert.IsType<FormatException>(ex.InnerException);
	}

	[Theory]
	[InlineData(0, 2)]
	[InlineData(3, 2)]
	public void a_layout_older_than_the_oldest_or_newer_than_the_target_is_refused(int from, int to)
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => UserConfigMigrations.Migrate(Settings(), from, to, [ToTwo]));
	}

	[Fact]
	public void null_arguments_are_refused()
	{
		Assert.Throws<ArgumentNullException>(() => UserConfigMigrations.Migrate(null!, 1, 1, []));
		Assert.Throws<ArgumentNullException>(() => UserConfigMigrations.Migrate(Settings(), 1, 1, null!));
	}

	/// <summary>Bumping <see cref="UserConfigVersion.Current"/> without its step - or adding a step for no layout - fails here.</summary>
	[Fact]
	public void the_real_steps_bring_every_layout_after_the_oldest_up_to_the_current_one()
	{
		Assert.True(UserConfigVersion.Oldest <= UserConfigVersion.Current);
		Assert.Equal(
			Enumerable.Range(UserConfigVersion.Oldest + 1, UserConfigVersion.Current - UserConfigVersion.Oldest),
			UserConfigMigrations.All.Select(step => step.ToVersion).Order());
		Assert.All(UserConfigMigrations.All, step => Assert.False(string.IsNullOrWhiteSpace(step.Description)));
	}

	// ---- the edits ----

	[Fact]
	public void move_renames_a_setting_and_carries_everything_below_it()
	{
		Dictionary<string, string> values = Settings(
			("Services.AiracService.VnasAlias.Combine", "Y"),
			("Services.AiracService.VnasAlias.Sources.1.Url", "https://example.com/a.txt"),
			("Services.AiracService.VnasAliasNote", "only its name starts the same"));

		Context(values).Move("Services.AiracService.VnasAlias", "Services.AiracService.ConcatenateAliases");

		Assert.Equal(
			Settings(
				("Services.AiracService.ConcatenateAliases.Combine", "Y"),
				("Services.AiracService.ConcatenateAliases.Sources.1.Url", "https://example.com/a.txt"),
				("Services.AiracService.VnasAliasNote", "only its name starts the same")),
			values);
	}

	/// <summary>Run twice, or over a file that already has the new setting: the new one wins, and nothing breaks.</summary>
	[Fact]
	public void move_never_overwrites_a_setting_already_in_the_new_place_and_does_nothing_with_nothing_to_move()
	{
		Dictionary<string, string> values = Settings(("General.Old", "old"), ("General.New", "new"));
		UserConfigMigrationContext context = Context(values);

		context.Move("General.Old", "General.New");
		context.Move("General.Old", "General.New");
		context.Move("General.Missing", "General.Elsewhere");

		Assert.Equal(Settings(("General.New", "new")), values);
	}

	[Fact]
	public void move_refuses_a_bad_path_or_a_place_inside_itself()
	{
		UserConfigMigrationContext context = Context(Settings(("General.A", "1")));

		Assert.Throws<ArgumentException>(() => context.Move("General.A", "General.A.Inner"));
		Assert.Throws<ArgumentException>(() => context.Move("General..A", "General.B"));
		Assert.Throws<ArgumentException>(() => context.Move("General.A", ".B"));
	}

	[Fact]
	public void change_value_rewrites_a_setting_that_is_there_and_nothing_else()
	{
		Dictionary<string, string> values = Settings(("General.Flag", "true"));
		UserConfigMigrationContext context = Context(values);

		context.ChangeValue("General.Flag", value => value == "true" ? "Y" : "N");
		context.ChangeValue("General.Missing", _ => "never called");

		Assert.Equal(Settings(("General.Flag", "Y")), values);
		Assert.Throws<ArgumentNullException>(() => context.ChangeValue("General.Flag", null!));
		Assert.Throws<InvalidOperationException>(() => context.ChangeValue("General.Flag", _ => null!));
	}

	[Fact]
	public void set_get_remove_and_keys_at_or_below_work_on_dotted_paths()
	{
		Dictionary<string, string> values = Settings(("General.List.1", "a"), ("General.List.2", "b"), ("General.ListNote", "c"));
		UserConfigMigrationContext context = Context(values);

		context.Set("General.Added", "d");

		Assert.Equal("d", context.Get("General.Added"));
		Assert.Null(context.Get("General.Nope"));
		Assert.Equal(["General.List.1", "General.List.2"], context.KeysAtOrBelow("General.List").Order());
		Assert.Same(values, context.Values);

		context.Remove("General.List");

		Assert.Equal(Settings(("General.ListNote", "c"), ("General.Added", "d")), values);
		Assert.Throws<ArgumentException>(() => context.Set("General.", "x"));
		Assert.Throws<ArgumentNullException>(() => context.Set("General.X", null!));
		Assert.Throws<ArgumentNullException>(() => context.KeysAtOrBelow(null!));
	}
}
