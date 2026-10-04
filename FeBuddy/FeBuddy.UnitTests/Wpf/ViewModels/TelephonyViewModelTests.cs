using FeBuddy.Wpf.ViewModels;
using FeBuddy.Wpf.ViewModels.Models;
using FeBuddy.Wpf.ViewModels.ServiceTabs;
using FeBuddy.Wpf.ViewModels.ServiceTabs.Models;

using FeBuddy.Core.Application.Airac.Models;
using FeBuddy.Core.Application.Airac.Telephony;
using FeBuddy.Core.Application.Airac.Telephony.Models;
using FeBuddy.Core.Infrastructure.Configuration;
using FeBuddy.Core.Infrastructure.Logging;
using FeBuddy.Core.Infrastructure.SharedData.Models;

namespace FeBuddy.UnitTests.Wpf.ViewModels;

/// <summary>
/// Covers the Virtual Airlines card of <see cref="TelephonyViewModel"/>: adding, editing, deleting
/// and cancelling in the editor, what the editor lets through and says, what is saved and sent to a
/// run, a hand-written entry that cannot be written, the Preview Settings row and the run summary -
/// against a throwaway config. No command that opens a dialog is run. Also the VATSIM-Radar Virtual
/// Airline List: the choice saved and sent, an exact match turned away with a warning, one that
/// differs let through with a heads-up, the user's own flagged, the copy's status, and downloading
/// it - through a stand-in, never the network.
/// </summary>
[Collection("AppLog")]
public sealed class TelephonyViewModelTests : IDisposable
{
	private const string Node = "Services.AiracService.Telephony";

	private readonly string _root = Path.Combine(Path.GetTempPath(), "FeBuddyTests_TelephonyTab_" + Guid.NewGuid().ToString("N"));

	/// <summary>Points the config and the log at a throwaway folder.</summary>
	public TelephonyViewModelTests()
	{
		AppLog.ConfigureForTesting(Path.Combine(_root, "logs"));
		UserConfigFile.ConfigureForTesting(Path.Combine(_root, "config"));
	}

	/// <summary>Restores the real config and log, and deletes the folder.</summary>
	public void Dispose()
	{
		UserConfigFile.ConfigureForTesting(null);
		AppLog.ConfigureForTesting(null);

		try
		{
			Directory.Delete(_root, recursive: true);
		}
		catch (IOException)
		{
			// Best-effort cleanup.
		}
	}

	/// <summary>Opens the editor to add and fills it, without confirming.</summary>
	private static void FillEditor(TelephonyViewModel tab, string designator, string telephony, string organization)
	{
		tab.AddVirtualAirlineCommand.Execute(null);
		tab.EditDesignator = designator;
		tab.EditTelephony = telephony;
		tab.EditOrganization = organization;
	}

	/// <summary>Adds a virtual airline the way the user does: open the editor, fill it, confirm.</summary>
	private static void Add(TelephonyViewModel tab, string designator, string telephony, string organization)
	{
		FillEditor(tab, designator, telephony, organization);
		tab.ConfirmVirtualAirlineCommand.Execute(null);
	}

	/// <summary>A tab with these virtual airlines added, not yet saved.</summary>
	private static TelephonyViewModel TabWith(params (string Designator, string Telephony, string Organization)[] airlines)
	{
		TelephonyViewModel tab = new();

		foreach ((string designator, string telephony, string organization) in airlines)
		{
			Add(tab, designator, telephony, organization);
		}

		return tab;
	}

	/// <summary>The list as one string per virtual airline: <c>DVA / DELTA / Delta Virtual</c>.</summary>
	private static string[] Rows(TelephonyViewModel tab) =>
		[.. tab.VirtualAirlines.Select(va => $"{va.Designator} / {va.Telephony} / {va.Organization}")];

	/// <summary>Writes one virtual airline's three keys straight into the config, as a hand-edited or imported file would hold them.</summary>
	private static void SaveToConfig(int number, string designator, string telephony, string organization)
	{
		UserConfigFile.TrySetValue($"{Node}.VirtualAirlines.{number}.Designator", designator);
		UserConfigFile.TrySetValue($"{Node}.VirtualAirlines.{number}.Telephony", telephony);
		UserConfigFile.TrySetValue($"{Node}.VirtualAirlines.{number}.Organization", organization);
	}

	// ---- start ----

	[Fact]
	public void a_new_tab_has_no_virtual_airlines_and_is_not_dirty()
	{
		TelephonyViewModel tab = new();

		Assert.Empty(tab.VirtualAirlines);
		Assert.False(tab.HasVirtualAirlines);
		Assert.False(tab.IsEditingVirtualAirline);
		Assert.Equal("Add", tab.ConfirmVirtualAirlineText);
		Assert.Equal(string.Empty, tab.VirtualAirlineEditorHint);
		Assert.False(tab.IsDirty);
		Assert.Null(tab.ValidationError);
		Assert.Equal(ServiceTabStatus.Ok, tab.Status);
	}

	// ---- add ----

	/// <summary>The card prints the 3LD and telephony upper case, so the list shows them that way; the organization is kept as typed.</summary>
	[Fact]
	public void adding_stores_the_3ld_and_telephony_upper_case_and_the_organization_as_typed()
	{
		TelephonyViewModel tab = new();

		Add(tab, " dva ", " delta ", "  Delta Virtual  ");

		VirtualAirlineItem item = Assert.Single(tab.VirtualAirlines);
		Assert.Equal("DVA", item.Designator);
		Assert.Equal("DELTA", item.Telephony);
		Assert.Equal("Delta Virtual", item.Organization);
		Assert.True(tab.HasVirtualAirlines);
		Assert.True(tab.IsDirty);
		Assert.Equal(ServiceTabStatus.Unsaved, tab.Status);
	}

	[Fact]
	public void adding_closes_and_clears_the_editor()
	{
		TelephonyViewModel tab = new();

		FillEditor(tab, "DVA", "DELTA", "Delta Virtual");
		Assert.True(tab.IsEditingVirtualAirline);

		tab.ConfirmVirtualAirlineCommand.Execute(null);

		Assert.False(tab.IsEditingVirtualAirline);
		Assert.Equal(string.Empty, tab.EditDesignator);
		Assert.Equal(string.Empty, tab.EditTelephony);
		Assert.Equal(string.Empty, tab.EditOrganization);
	}

	[Fact]
	public void virtual_airlines_are_listed_in_the_order_they_were_added()
	{
		TelephonyViewModel tab = TabWith(("ZZV", "ZULU", "Zulu Virtual"), ("AAV", "ALPHA", "Alpha Virtual"));

		Assert.Equal(["ZZV / ZULU / Zulu Virtual", "AAV / ALPHA / Alpha Virtual"], Rows(tab));
	}

	[Fact]
	public void nothing_is_written_to_the_config_until_the_tab_is_saved()
	{
		TelephonyViewModel tab = TabWith(("DVA", "DELTA", "Delta Virtual"));

		Assert.Null(UserConfigFile.GetValue($"{Node}.VirtualAirlines.1.Designator"));
		Assert.True(tab.IsDirty);
	}

	[Fact]
	public void adding_and_deleting_raise_has_virtual_airlines()
	{
		TelephonyViewModel tab = new();
		List<string?> raised = [];
		tab.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

		Add(tab, "DVA", "DELTA", "Delta Virtual");
		Assert.Contains(nameof(TelephonyViewModel.HasVirtualAirlines), raised);

		raised.Clear();
		tab.DeleteVirtualAirlineCommand.Execute(tab.VirtualAirlines[0]);

		Assert.Contains(nameof(TelephonyViewModel.HasVirtualAirlines), raised);
	}

	// ---- the editor: what can be confirmed, and what it says ----

	[Theory]
	[InlineData("D1", "DELTA", "Delta Virtual", "The 3LD is three letters, A to Z.")]
	[InlineData("1", "DELTA", "Delta Virtual", "The 3LD is three letters, A to Z.")]
	[InlineData("DVAX", "DELTA", "Delta Virtual", "The 3LD is three letters, A to Z.")]
	[InlineData("DVA", "!!", "Delta Virtual", "The telephony needs a letter or a digit.")]
	[InlineData("D1", "!!", "Delta Virtual", "The 3LD is three letters, A to Z.")]
	public void a_virtual_airline_that_cannot_be_written_cannot_be_confirmed_and_the_hint_says_why(
		string designator, string telephony, string organization, string expectedHint)
	{
		TelephonyViewModel tab = new();

		FillEditor(tab, designator, telephony, organization);

		Assert.False(tab.ConfirmVirtualAirlineCommand.CanExecute(null));
		Assert.Equal(expectedHint, tab.VirtualAirlineEditorHint);
	}

	/// <summary>A box still being filled in is not an error: nothing to say, but nothing to add yet either.</summary>
	[Theory]
	[InlineData("", "", "")]
	[InlineData("D", "DELTA", "Delta Virtual")]
	[InlineData("DV", "DELTA", "Delta Virtual")]
	[InlineData("DVA", "", "")]
	[InlineData("DVA", "DELTA", "")]
	[InlineData("DVA", "DELTA", "   ")]
	[InlineData("   ", "DELTA", "Delta Virtual")]
	[InlineData("", "", "Delta Virtual")]
	public void a_virtual_airline_still_being_filled_in_cannot_be_confirmed_and_has_no_hint(
		string designator, string telephony, string organization)
	{
		TelephonyViewModel tab = new();

		FillEditor(tab, designator, telephony, organization);

		Assert.False(tab.ConfirmVirtualAirlineCommand.CanExecute(null));
		Assert.Equal(string.Empty, tab.VirtualAirlineEditorHint);
	}

	[Fact]
	public void a_complete_virtual_airline_can_be_confirmed_with_no_hint()
	{
		TelephonyViewModel tab = new();

		FillEditor(tab, "dva", "delta", "Delta Virtual");

		Assert.True(tab.ConfirmVirtualAirlineCommand.CanExecute(null));
		Assert.Equal(string.Empty, tab.VirtualAirlineEditorHint);
	}

	[Fact]
	public void the_hint_follows_the_editor_as_it_is_typed_in()
	{
		TelephonyViewModel tab = new();
		tab.AddVirtualAirlineCommand.Execute(null);
		List<string?> raised = [];
		tab.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

		tab.EditDesignator = "D1";
		tab.EditTelephony = "DELTA";
		tab.EditOrganization = "Delta Virtual";

		Assert.Equal(3, raised.Count(name => name == nameof(TelephonyViewModel.VirtualAirlineEditorHint)));
	}

	/// <summary>The card prints upper case, so a change of case alone is the same virtual airline.</summary>
	[Fact]
	public void a_duplicate_ignoring_case_cannot_be_confirmed_and_the_hint_says_so()
	{
		TelephonyViewModel tab = TabWith(("DVA", "DELTA", "Delta Virtual"));

		FillEditor(tab, "dva", "delta", "DELTA VIRTUAL");

		Assert.False(tab.ConfirmVirtualAirlineCommand.CanExecute(null));
		Assert.Equal("Already in the list.", tab.VirtualAirlineEditorHint);
	}

	/// <summary>Same 3LD and telephony under another organization is a different virtual airline.</summary>
	[Fact]
	public void the_same_3ld_and_telephony_with_another_organization_is_not_a_duplicate()
	{
		TelephonyViewModel tab = TabWith(("DVA", "DELTA", "Delta Virtual"));

		FillEditor(tab, "DVA", "DELTA", "Rustic Virtual");

		Assert.True(tab.ConfirmVirtualAirlineCommand.CanExecute(null));
		Assert.Equal(string.Empty, tab.VirtualAirlineEditorHint);
	}

	[Fact]
	public void confirming_something_that_cannot_be_added_adds_nothing()
	{
		TelephonyViewModel tab = new();
		FillEditor(tab, "D1", "DELTA", "Delta Virtual");

		tab.ConfirmVirtualAirlineCommand.Execute(null);

		Assert.Empty(tab.VirtualAirlines);
		Assert.True(tab.IsEditingVirtualAirline);
		Assert.False(tab.IsDirty);
	}

	[Fact]
	public void the_confirm_button_says_add_for_a_new_virtual_airline_and_save_while_editing()
	{
		TelephonyViewModel tab = TabWith(("DVA", "DELTA", "Delta Virtual"));
		Assert.Equal("Add", tab.ConfirmVirtualAirlineText);

		tab.AddVirtualAirlineCommand.Execute(null);
		Assert.Equal("Add", tab.ConfirmVirtualAirlineText);

		tab.EditVirtualAirlineCommand.Execute(tab.VirtualAirlines[0]);
		Assert.Equal("Save", tab.ConfirmVirtualAirlineText);

		tab.CancelVirtualAirlineCommand.Execute(null);
		Assert.Equal("Add", tab.ConfirmVirtualAirlineText);
	}

	[Fact]
	public void switching_between_adding_and_editing_raises_the_confirm_button_text()
	{
		TelephonyViewModel tab = TabWith(("DVA", "DELTA", "Delta Virtual"));
		List<string?> raised = [];
		tab.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

		tab.EditVirtualAirlineCommand.Execute(tab.VirtualAirlines[0]);

		Assert.Contains(nameof(TelephonyViewModel.ConfirmVirtualAirlineText), raised);
	}

	// ---- edit ----

	[Fact]
	public void edit_opens_the_editor_with_that_virtual_airlines_values()
	{
		TelephonyViewModel tab = TabWith(("DVA", "DELTA", "Delta Virtual"), ("DEV", "DEVIL AIR", "Rustic Virtual"));

		tab.EditVirtualAirlineCommand.Execute(tab.VirtualAirlines[1]);

		Assert.True(tab.IsEditingVirtualAirline);
		Assert.Equal("DEV", tab.EditDesignator);
		Assert.Equal("DEVIL AIR", tab.EditTelephony);
		Assert.Equal("Rustic Virtual", tab.EditOrganization);
	}

	/// <summary>Save changes that line where it stands: the same item, the same place in the list.</summary>
	[Fact]
	public void saving_an_edit_changes_that_item_in_place()
	{
		TelephonyViewModel tab = TabWith(("DVA", "DELTA", "Delta Virtual"), ("DEV", "DEVIL AIR", "Rustic Virtual"));
		VirtualAirlineItem first = tab.VirtualAirlines[0];
		tab.Save();

		tab.EditVirtualAirlineCommand.Execute(first);
		tab.EditDesignator = "dav";
		tab.EditTelephony = " delta air ";
		tab.EditOrganization = "  Delta Air Virtual ";
		tab.ConfirmVirtualAirlineCommand.Execute(null);

		Assert.Same(first, tab.VirtualAirlines[0]);
		Assert.Equal(["DAV / DELTA AIR / Delta Air Virtual", "DEV / DEVIL AIR / Rustic Virtual"], Rows(tab));
		Assert.False(tab.IsEditingVirtualAirline);
		Assert.True(tab.IsDirty);
	}

	/// <summary>Its own entry is not "already in the list": it is the one being changed.</summary>
	[Fact]
	public void an_item_edited_to_its_own_values_is_not_a_duplicate()
	{
		TelephonyViewModel tab = TabWith(("DVA", "DELTA", "Delta Virtual"), ("DEV", "DEVIL AIR", "Rustic Virtual"));

		tab.EditVirtualAirlineCommand.Execute(tab.VirtualAirlines[0]);

		Assert.True(tab.ConfirmVirtualAirlineCommand.CanExecute(null));
		Assert.Equal(string.Empty, tab.VirtualAirlineEditorHint);

		tab.ConfirmVirtualAirlineCommand.Execute(null);

		Assert.Equal(["DVA / DELTA / Delta Virtual", "DEV / DEVIL AIR / Rustic Virtual"], Rows(tab));
	}

	[Fact]
	public void an_item_edited_into_another_items_values_is_a_duplicate()
	{
		TelephonyViewModel tab = TabWith(("DVA", "DELTA", "Delta Virtual"), ("DEV", "DEVIL AIR", "Rustic Virtual"));

		tab.EditVirtualAirlineCommand.Execute(tab.VirtualAirlines[1]);
		tab.EditDesignator = "DVA";
		tab.EditTelephony = "DELTA";
		tab.EditOrganization = "Delta Virtual";

		Assert.False(tab.ConfirmVirtualAirlineCommand.CanExecute(null));
		Assert.Equal("Already in the list.", tab.VirtualAirlineEditorHint);
	}

	[Fact]
	public void editing_something_that_is_not_in_the_list_opens_nothing()
	{
		TelephonyViewModel tab = TabWith(("DVA", "DELTA", "Delta Virtual"));

		tab.EditVirtualAirlineCommand.Execute(new VirtualAirlineItem("DEV", "DEVIL AIR", "Rustic Virtual"));
		tab.EditVirtualAirlineCommand.Execute(null);

		Assert.False(tab.IsEditingVirtualAirline);
	}

	// ---- delete ----

	[Fact]
	public void delete_removes_the_virtual_airline_and_marks_the_tab_unsaved()
	{
		TelephonyViewModel tab = TabWith(("DVA", "DELTA", "Delta Virtual"), ("DEV", "DEVIL AIR", "Rustic Virtual"));
		tab.Save();
		Assert.False(tab.IsDirty);

		tab.DeleteVirtualAirlineCommand.Execute(tab.VirtualAirlines[0]);

		Assert.Equal(["DEV / DEVIL AIR / Rustic Virtual"], Rows(tab));
		Assert.True(tab.IsDirty);
	}

	[Fact]
	public void deleting_the_item_being_edited_closes_the_editor()
	{
		TelephonyViewModel tab = TabWith(("DVA", "DELTA", "Delta Virtual"), ("DEV", "DEVIL AIR", "Rustic Virtual"));
		VirtualAirlineItem editing = tab.VirtualAirlines[0];
		tab.EditVirtualAirlineCommand.Execute(editing);

		tab.DeleteVirtualAirlineCommand.Execute(editing);

		Assert.False(tab.IsEditingVirtualAirline);
		Assert.Equal(string.Empty, tab.EditDesignator);
		Assert.Equal("Add", tab.ConfirmVirtualAirlineText);
		Assert.Equal(["DEV / DEVIL AIR / Rustic Virtual"], Rows(tab));
	}

	/// <summary>The editor follows the item, not its position: deleting a line above it must not move the edit onto another line.</summary>
	[Fact]
	public void deleting_an_earlier_item_keeps_the_editor_on_the_same_item()
	{
		TelephonyViewModel tab = TabWith(("DVA", "DELTA", "Delta Virtual"), ("DEV", "DEVIL AIR", "Rustic Virtual"));
		tab.EditVirtualAirlineCommand.Execute(tab.VirtualAirlines[1]);

		tab.DeleteVirtualAirlineCommand.Execute(tab.VirtualAirlines[0]);
		tab.EditOrganization = "Changed Virtual";
		tab.ConfirmVirtualAirlineCommand.Execute(null);

		Assert.Equal(["DEV / DEVIL AIR / Changed Virtual"], Rows(tab));
	}

	[Fact]
	public void deleting_something_that_is_not_in_the_list_changes_nothing()
	{
		TelephonyViewModel tab = TabWith(("DVA", "DELTA", "Delta Virtual"));
		tab.Save();

		tab.DeleteVirtualAirlineCommand.Execute(new VirtualAirlineItem("DEV", "DEVIL AIR", "Rustic Virtual"));
		tab.DeleteVirtualAirlineCommand.Execute(null);

		Assert.Equal(["DVA / DELTA / Delta Virtual"], Rows(tab));
		Assert.False(tab.IsDirty);
	}

	// ---- cancel ----

	[Fact]
	public void cancel_closes_the_editor_without_changing_anything()
	{
		TelephonyViewModel tab = TabWith(("DVA", "DELTA", "Delta Virtual"));
		tab.Save();

		tab.EditVirtualAirlineCommand.Execute(tab.VirtualAirlines[0]);
		tab.EditDesignator = "XYZ";
		tab.EditOrganization = "Something Else";
		tab.CancelVirtualAirlineCommand.Execute(null);

		Assert.False(tab.IsEditingVirtualAirline);
		Assert.Equal(string.Empty, tab.EditDesignator);
		Assert.Equal(string.Empty, tab.EditOrganization);
		Assert.Equal(["DVA / DELTA / Delta Virtual"], Rows(tab));
		Assert.False(tab.IsDirty);
	}

	[Fact]
	public void cancelling_an_add_leaves_the_list_as_it_was()
	{
		TelephonyViewModel tab = new();

		FillEditor(tab, "DVA", "DELTA", "Delta Virtual");
		tab.CancelVirtualAirlineCommand.Execute(null);

		Assert.Empty(tab.VirtualAirlines);
		Assert.False(tab.IsEditingVirtualAirline);
		Assert.False(tab.IsDirty);
	}

	// ---- save and load ----

	[Fact]
	public void saved_virtual_airlines_come_back_in_the_same_order_and_the_tab_is_clean()
	{
		TelephonyViewModel tab = TabWith(("ZZV", "ZULU", "Zulu Virtual"), ("AAV", "ALPHA", "Alpha Virtual"));
		Assert.True(tab.Save());
		Assert.False(tab.IsDirty);

		TelephonyViewModel reloaded = new();

		Assert.Equal(["ZZV / ZULU / Zulu Virtual", "AAV / ALPHA / Alpha Virtual"], Rows(reloaded));
		Assert.False(reloaded.IsDirty);
		Assert.Equal(ServiceTabStatus.Ok, reloaded.Status);
	}

	[Fact]
	public void saving_writes_the_list_as_numbered_keys_from_1()
	{
		TelephonyViewModel tab = TabWith(("DVA", "DELTA", "Delta Virtual"), ("DEV", "DEVIL AIR", "Rustic Virtual"));

		Assert.True(tab.Save());

		Assert.Equal("DVA", UserConfigFile.GetValue($"{Node}.VirtualAirlines.1.Designator"));
		Assert.Equal("DELTA", UserConfigFile.GetValue($"{Node}.VirtualAirlines.1.Telephony"));
		Assert.Equal("Delta Virtual", UserConfigFile.GetValue($"{Node}.VirtualAirlines.1.Organization"));
		Assert.Equal("DEV", UserConfigFile.GetValue($"{Node}.VirtualAirlines.2.Designator"));
		Assert.Equal("DEVIL AIR", UserConfigFile.GetValue($"{Node}.VirtualAirlines.2.Telephony"));
		Assert.Equal("Rustic Virtual", UserConfigFile.GetValue($"{Node}.VirtualAirlines.2.Organization"));
	}

	/// <summary>A removed virtual airline must not leave its numbered keys behind, or the next load would bring it back.</summary>
	[Fact]
	public void after_deleting_one_and_saving_no_keys_of_the_last_number_remain()
	{
		TelephonyViewModel tab = TabWith(("DVA", "DELTA", "Delta Virtual"), ("DEV", "DEVIL AIR", "Rustic Virtual"));
		Assert.True(tab.Save());

		tab.DeleteVirtualAirlineCommand.Execute(tab.VirtualAirlines[0]);
		Assert.True(tab.Save());

		string[] keys = [.. UserConfigFile.SnapshotValues().Keys.Where(key => key.StartsWith($"{Node}.VirtualAirlines.", StringComparison.Ordinal))];
		Assert.DoesNotContain(keys, key => key.StartsWith($"{Node}.VirtualAirlines.2.", StringComparison.Ordinal));
		Assert.Equal(3, keys.Length);
		Assert.Equal("DEV", UserConfigFile.GetValue($"{Node}.VirtualAirlines.1.Designator"));

		Assert.Equal(["DEV / DEVIL AIR / Rustic Virtual"], Rows(new TelephonyViewModel()));
	}

	[Fact]
	public void deleting_every_virtual_airline_and_saving_leaves_no_keys()
	{
		TelephonyViewModel tab = TabWith(("DVA", "DELTA", "Delta Virtual"));
		Assert.True(tab.Save());

		tab.DeleteVirtualAirlineCommand.Execute(tab.VirtualAirlines[0]);
		Assert.True(tab.Save());

		Assert.DoesNotContain(UserConfigFile.SnapshotValues().Keys, key => key.StartsWith($"{Node}.VirtualAirlines.", StringComparison.Ordinal));
		Assert.Empty(new TelephonyViewModel().VirtualAirlines);
	}

	/// <summary>A hand-edited file may number them any way: the tab lists them in number order, and leaves out a number with nothing in it.</summary>
	[Fact]
	public void saved_virtual_airlines_load_in_number_order_and_a_blank_number_is_left_out()
	{
		SaveToConfig(10, "ten", "ten call", "Ten Virtual");
		SaveToConfig(2, "dva", "delta", "Delta Virtual");
		SaveToConfig(5, "", "  ", "");

		TelephonyViewModel tab = new();

		Assert.Equal(["DVA / DELTA / Delta Virtual", "TEN / TEN CALL / Ten Virtual"], Rows(tab));
		Assert.False(tab.IsDirty);
	}

	[Fact]
	public void discarding_changes_brings_back_the_saved_list_and_closes_the_editor()
	{
		TelephonyViewModel tab = TabWith(("DVA", "DELTA", "Delta Virtual"));
		Assert.True(tab.Save());

		Add(tab, "DEV", "DEVIL AIR", "Rustic Virtual");
		tab.EditVirtualAirlineCommand.Execute(tab.VirtualAirlines[0]);
		Assert.True(tab.IsDirty);

		tab.RevertChanges();

		Assert.Equal(["DVA / DELTA / Delta Virtual"], Rows(tab));
		Assert.False(tab.IsEditingVirtualAirline);
		Assert.False(tab.IsDirty);
	}

	// ---- a hand-written entry that cannot be written ----

	[Fact]
	public void a_hand_written_entry_with_a_bad_3ld_loads_and_the_tab_is_invalid_naming_it()
	{
		SaveToConfig(1, "D1", "DELTA", "Delta Virtual");

		TelephonyViewModel tab = new();

		Assert.Equal("D1", Assert.Single(tab.VirtualAirlines).Designator);
		Assert.Equal(ServiceTabStatus.Invalid, tab.Status);
		Assert.NotNull(tab.ValidationError);
		Assert.Contains("Virtual airline 1", tab.ValidationError);
		Assert.Contains("3LD", tab.ValidationError);
		Assert.False(tab.IsDirty);
	}

	/// <summary>Counted by position in the list, as the run will count them once the tab renumbers them from 1.</summary>
	[Fact]
	public void a_bad_entry_is_named_by_its_place_in_the_list()
	{
		SaveToConfig(5, "DVA", "DELTA", "Delta Virtual");
		SaveToConfig(9, "DEV", "!!", "Rustic Virtual");

		TelephonyViewModel tab = new();

		Assert.Equal(ServiceTabStatus.Invalid, tab.Status);
		Assert.Contains("Virtual airline 2 (DEV)", tab.ValidationError);
		Assert.Contains("telephony", tab.ValidationError);
	}

	[Fact]
	public void a_tab_with_a_bad_entry_cannot_be_saved()
	{
		SaveToConfig(1, "DVA", "DELTA", "");

		TelephonyViewModel tab = new();

		Assert.False(tab.Save());
		Assert.Contains("no virtual organization", tab.ValidationError);
	}

	[Fact]
	public void editing_a_bad_entry_into_a_valid_one_clears_the_error()
	{
		SaveToConfig(1, "D1", "DELTA", "Delta Virtual");
		TelephonyViewModel tab = new();
		Assert.Equal(ServiceTabStatus.Invalid, tab.Status);

		tab.EditVirtualAirlineCommand.Execute(tab.VirtualAirlines[0]);
		tab.EditDesignator = "DVA";
		tab.ConfirmVirtualAirlineCommand.Execute(null);

		Assert.Null(tab.ValidationError);
		Assert.Equal(ServiceTabStatus.Unsaved, tab.Status);
		Assert.True(tab.Save());
	}

	[Fact]
	public void deleting_a_bad_entry_clears_the_error()
	{
		SaveToConfig(1, "D1", "DELTA", "Delta Virtual");
		TelephonyViewModel tab = new();

		tab.DeleteVirtualAirlineCommand.Execute(tab.VirtualAirlines[0]);

		Assert.Null(tab.ValidationError);
		Assert.True(tab.Save());
	}

	// ---- what is sent to a run ----

	[Fact]
	public void the_settings_block_numbers_the_virtual_airlines_from_1_in_list_order()
	{
		TelephonyViewModel tab = TabWith(("ZZV", "ZULU", "Zulu Virtual"), ("AAV", "ALPHA", "Alpha Virtual"));

		IReadOnlyDictionary<string, string> block = tab.BuildSettingsBlock();

		Assert.Equal("ZZV", block["VirtualAirlines.1.Designator"]);
		Assert.Equal("ZULU", block["VirtualAirlines.1.Telephony"]);
		Assert.Equal("Zulu Virtual", block["VirtualAirlines.1.Organization"]);
		Assert.Equal("AAV", block["VirtualAirlines.2.Designator"]);
		Assert.Equal("ALPHA", block["VirtualAirlines.2.Telephony"]);
		Assert.Equal("Alpha Virtual", block["VirtualAirlines.2.Organization"]);
		Assert.DoesNotContain(block.Keys, key => key.StartsWith("VirtualAirlines.3.", StringComparison.Ordinal));
	}

	/// <summary>The block is what the run's parser reads, so every key it carries must be one the parser knows.</summary>
	[Fact]
	public void the_settings_block_is_read_back_by_the_parser_without_warnings()
	{
		TelephonyViewModel tab = TabWith(("DVA", "DELTA", "Delta Virtual"), ("DEV", "DEVIL AIR", "Rustic Virtual"));
		Dictionary<string, string> block = new(tab.BuildSettingsBlock(), StringComparer.OrdinalIgnoreCase)
		{
			["OutputDirectory"] = Path.Combine(_root, "AIRAC_2610"),
		};

		TelephonySettingsParseResult result = TelephonySettingsParser.Parse(block);

		Assert.Empty(result.Messages);
		Assert.Equal(
			[new VirtualAirline("DVA", "DELTA", "Delta Virtual"), new VirtualAirline("DEV", "DEVIL AIR", "Rustic Virtual")],
			result.Settings.VirtualAirlines);
	}

	[Fact]
	public void with_no_virtual_airlines_the_settings_block_has_no_virtual_airline_keys()
	{
		TelephonyViewModel tab = new();

		Assert.DoesNotContain(tab.BuildSettingsBlock().Keys, key => key.StartsWith("VirtualAirlines.", StringComparison.OrdinalIgnoreCase));
	}

	// ---- preview and run summary ----

	[Fact]
	public void the_preview_says_none_with_no_virtual_airlines()
	{
		TelephonyViewModel tab = new();

		string virtualAirlines = tab.BuildPreviewSummary()[0].Rows.Single(row => row.Label == "Virtual airlines").Value;

		Assert.Equal("None", virtualAirlines);
	}

	[Fact]
	public void the_preview_lists_each_virtual_airline_with_its_telephony_and_organization()
	{
		TelephonyViewModel tab = TabWith(("DVA", "DELTA", "Delta Virtual"), ("DEV", "DEVIL AIR", "Rustic Virtual"));

		string virtualAirlines = tab.BuildPreviewSummary()[0].Rows.Single(row => row.Label == "Virtual airlines").Value;

		Assert.Equal("DVA (DELTA, Delta Virtual), DEV (DEVIL AIR, Rustic Virtual)", virtualAirlines);
	}

	/// <summary>An <see cref="AiracServiceResult"/> carrying only a Telephony result, with these counts.</summary>
	private static AiracServiceResult RunResult(
		int virtualAirlines, string? aliasFilePath = @"C:\Out\AIRAC_2610\Aliases\Telephony.txt", int fromVatsimRadar = 0) => new()
		{
			OutputDirectory = @"C:\Out\AIRAC_2610",
			Messages = [],
			Elapsed = TimeSpan.Zero,
			Telephony = new TelephonyServiceResult
			{
				Messages = [],
				Elapsed = TimeSpan.Zero,
				IcaoAssignmentCount = 3,
				SpecialCallSignCount = 2,
				VirtualAirlineCount = virtualAirlines,
				VatsimRadarVirtualAirlineCount = fromVatsimRadar,
				NoDesignatorCount = 0,
				NoTelephonyCount = 0,
				ExpiredCount = 0,
				AliasFilePath = aliasFilePath,
				AliasCommandCount = 9,
			},
		};

	[Fact]
	public void the_run_summary_names_the_virtual_airlines_only_when_there_were_some()
	{
		TelephonyViewModel tab = new();

		SubServiceRunResult someVirtualAirlines = tab.DescribeRunResult(RunResult(virtualAirlines: 4))!;
		SubServiceRunResult noVirtualAirlines = tab.DescribeRunResult(RunResult(virtualAirlines: 0))!;

		Assert.Equal("Telephony", someVirtualAirlines.Name);
		Assert.Equal(
			"Telephony.txt: 9 command(s) for 3 ICAO operator(s), 2 U.S. special call sign(s) and 4 virtual airline(s)",
			someVirtualAirlines.Summary);
		Assert.Equal("Telephony.txt: 9 command(s) for 3 ICAO operator(s) and 2 U.S. special call sign(s)", noVirtualAirlines.Summary);
	}

	[Fact]
	public void the_run_summary_says_nothing_written_when_there_is_no_file()
	{
		TelephonyViewModel tab = new();

		SubServiceRunResult run = tab.DescribeRunResult(RunResult(virtualAirlines: 2, aliasFilePath: null))!;

		Assert.Equal("Nothing written", run.Summary);
	}

	[Fact]
	public void there_is_no_run_summary_when_telephony_was_not_part_of_the_run()
	{
		TelephonyViewModel tab = new();
		AiracServiceResult result = new() { OutputDirectory = @"C:\Out\AIRAC_2610", Messages = [], Elapsed = TimeSpan.Zero };

		Assert.Null(tab.DescribeRunResult(result));
	}

	[Fact]
	public void the_run_summary_says_how_many_virtual_airlines_came_from_the_vatsim_radar_list()
	{
		SubServiceRunResult run = new TelephonyViewModel().DescribeRunResult(RunResult(virtualAirlines: 250, fromVatsimRadar: 248))!;

		Assert.Equal(
			"Telephony.txt: 9 command(s) for 3 ICAO operator(s), 2 U.S. special call sign(s) and 250 virtual airline(s) (248 from the VATSIM-Radar list)",
			run.Summary);
	}

	// ---- the VATSIM-Radar Virtual Airline List ----

	private static readonly DateTime ListDownloadedUtc = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);

	/// <summary>A copy of the list holding these virtual airlines, downloaded <see cref="ListDownloadedUtc"/>.</summary>
	private static VatsimRadarCopy ListOf(params (string Designator, string Telephony, string Organization)[] airlines) =>
		new([.. airlines.Select(va => new VirtualAirline(va.Designator, va.Telephony, va.Organization))], ListDownloadedUtc);

	/// <summary>A tab using this copy of the list, whose downloads count how often they are asked for and never touch the network.</summary>
	private static (TelephonyViewModel Tab, Func<int> Downloads) TabUsing(VatsimRadarCopy? copy, SharedDataRefreshResult? downloadResult = null)
	{
		int downloads = 0;
		TelephonyViewModel tab = new()
		{
			RefreshVatsimRadarList = _ =>
			{
				downloads++;
				return Task.FromResult(downloadResult ?? new SharedDataRefreshResult(null, null, "offline"));
			},
		};

		tab.UseVatsimRadarCopy(copy);
		return (tab, () => downloads);
	}

	[Fact]
	public void the_vatsim_radar_list_is_off_to_start_with_and_its_choice_is_saved_loaded_and_sent()
	{
		(TelephonyViewModel tab, _) = TabUsing(ListOf(("DAL", "Delta", "Fly Delta Virtual")));

		Assert.False(tab.IncludeVatsimRadarList);
		Assert.Equal("N", tab.BuildSettingsBlock()["IncludeVatsimRadarVirtualAirlines"]);

		tab.IncludeVatsimRadarList = true;

		Assert.True(tab.IsDirty);
		Assert.Equal("Y", tab.BuildSettingsBlock()["IncludeVatsimRadarVirtualAirlines"]);
		Assert.True(tab.Save());
		Assert.Equal("Y", UserConfigFile.GetValue($"{Node}.IncludeVatsimRadarVirtualAirlines"));

		TelephonyViewModel reloaded = new();
		Assert.True(reloaded.IncludeVatsimRadarList);
		Assert.False(reloaded.IsDirty);
	}

	/// <summary>The user's own warning and refusal: exactly the same as one on the list - ignoring case - can't be added.</summary>
	[Fact]
	public void with_the_list_included_a_virtual_airline_exactly_on_it_is_turned_away_with_a_warning()
	{
		(TelephonyViewModel tab, _) = TabUsing(ListOf(("DAL", "Delta", "Fly Delta Virtual")));
		tab.IncludeVatsimRadarList = true;

		FillEditor(tab, "dal", "DELTA", "fly delta virtual");

		Assert.Equal(
			"Already on the VATSIM-Radar Virtual Airline List, which you've chosen to include, as DAL · DELTA · Fly Delta Virtual. " +
			"It's written from there, so it can't be added again.",
			tab.VirtualAirlineEditorHint);
		Assert.False(tab.ConfirmVirtualAirlineCommand.CanExecute(null));

		tab.ConfirmVirtualAirlineCommand.Execute(null);
		Assert.Empty(tab.VirtualAirlines);
	}

	/// <summary>Not exactly the same - another virtual organization here - is the user's to add, with a heads-up that both are written.</summary>
	[Fact]
	public void with_the_list_included_one_that_differs_at_all_can_be_added_with_a_heads_up()
	{
		(TelephonyViewModel tab, _) = TabUsing(ListOf(("DAL", "Delta", "Fly Delta Virtual")));
		tab.IncludeVatsimRadarList = true;

		FillEditor(tab, "DAL", "DELTA", "Delta Virtual Airlines");

		Assert.Equal(string.Empty, tab.VirtualAirlineEditorHint);
		Assert.Equal(
			"The VATSIM-Radar Virtual Airline List also has DAL · DELTA, for Fly Delta Virtual. Yours is written too, with a card of its own.",
			tab.VirtualAirlineEditorNote);
		Assert.True(tab.ConfirmVirtualAirlineCommand.CanExecute(null));

		tab.ConfirmVirtualAirlineCommand.Execute(null);
		Assert.Equal(["DAL / DELTA / Delta Virtual Airlines"], Rows(tab));
		Assert.False(tab.VirtualAirlines[0].IsOnVatsimRadarList);
	}

	[Fact]
	public void with_the_list_not_included_anything_can_be_added_and_nothing_is_said_about_it()
	{
		(TelephonyViewModel tab, _) = TabUsing(ListOf(("DAL", "Delta", "Fly Delta Virtual")));

		FillEditor(tab, "DAL", "DELTA", "Fly Delta Virtual");

		Assert.Equal(string.Empty, tab.VirtualAirlineEditorHint);
		Assert.Equal(string.Empty, tab.VirtualAirlineEditorNote);
		Assert.True(tab.ConfirmVirtualAirlineCommand.CanExecute(null));
	}

	/// <summary>One added before the list was included is flagged, not removed: the run writes it once.</summary>
	[Fact]
	public void one_of_yours_that_is_on_the_list_is_flagged_while_the_list_is_included()
	{
		(TelephonyViewModel tab, _) = TabUsing(ListOf(("DAL", "Delta", "Fly Delta Virtual")));
		Add(tab, "DAL", "DELTA", "Fly Delta Virtual");
		Add(tab, "DVA", "DELTA", "Delta Virtual");

		Assert.All(tab.VirtualAirlines, item => Assert.False(item.IsOnVatsimRadarList));

		tab.IncludeVatsimRadarList = true;
		Assert.Equal([true, false], tab.VirtualAirlines.Select(item => item.IsOnVatsimRadarList));

		tab.IncludeVatsimRadarList = false;
		Assert.All(tab.VirtualAirlines, item => Assert.False(item.IsOnVatsimRadarList));
	}

	[Fact]
	public void the_lists_status_says_how_old_the_copy_is_and_how_many_it_has_or_that_there_is_none()
	{
		(TelephonyViewModel withCopy, _) = TabUsing(ListOf(("DAL", "Delta", "Fly Delta Virtual"), ("OCN", "Ocean", "vOCN")));
		(TelephonyViewModel withoutCopy, _) = TabUsing(null);

		Assert.Equal(
			$"FE-Buddy's copy is from {ListDownloadedUtc.ToLocalTime():d MMM yyyy}: 2 virtual airlines. Every run downloads the latest list first.",
			withCopy.VatsimRadarListStatus);
		Assert.Equal(
			"FE-Buddy has no copy yet, so a virtual airline you add can't be checked against it. Every run downloads the latest list first.",
			withoutCopy.VatsimRadarListStatus);
	}

	/// <summary>Ticking the box with no copy fetches one, so the editor has something to check against; with a copy it doesn't.</summary>
	[Fact]
	public void ticking_the_box_downloads_the_list_only_when_there_is_no_copy()
	{
		(TelephonyViewModel withoutCopy, Func<int> downloadsWithout) = TabUsing(null);
		(TelephonyViewModel withCopy, Func<int> downloadsWith) = TabUsing(ListOf(("DAL", "Delta", "Fly Delta Virtual")));

		withoutCopy.IncludeVatsimRadarList = true;
		withCopy.IncludeVatsimRadarList = true;

		Assert.Equal(1, downloadsWithout());
		Assert.Equal(0, downloadsWith());
		Assert.Contains("The latest couldn't be downloaded (offline).", withoutCopy.VatsimRadarListStatus, StringComparison.Ordinal);
		Assert.False(withoutCopy.IsDownloadingVatsimRadarList);
	}

	[Fact]
	public async Task a_download_uses_the_fresh_copy_and_then_checks_against_it()
	{
		string path = Path.Combine(_root, "vatsim_radar_airlines.json");
		Directory.CreateDirectory(_root);
		File.WriteAllText(path, """[{ "icao": "OCN", "name": "vOCN", "callsign": "Ocean", "virtual": true }]""");
		(TelephonyViewModel tab, Func<int> downloads) = TabUsing(null, new SharedDataRefreshResult(path, DateTime.UtcNow, FailureReason: null));
		tab.IncludeVatsimRadarList = true;

		await tab.DownloadVatsimRadarListAsync();

		Assert.Equal(2, downloads());
		Assert.StartsWith("FE-Buddy's copy is from ", tab.VatsimRadarListStatus, StringComparison.Ordinal);
		Assert.Contains(": 1 virtual airlines.", tab.VatsimRadarListStatus, StringComparison.Ordinal);
		Assert.DoesNotContain("couldn't", tab.VatsimRadarListStatus, StringComparison.Ordinal);

		FillEditor(tab, "OCN", "OCEAN", "vOCN");
		Assert.False(tab.ConfirmVirtualAirlineCommand.CanExecute(null));
	}

	[Fact]
	public async Task a_download_that_throws_keeps_the_copy_and_says_why()
	{
		(TelephonyViewModel tab, _) = TabUsing(ListOf(("DAL", "Delta", "Fly Delta Virtual")));
		tab.RefreshVatsimRadarList = _ => throw new InvalidOperationException("no network stack");

		await tab.DownloadVatsimRadarListAsync();

		Assert.Contains(": 1 virtual airlines. The latest couldn't be downloaded (no network stack).", tab.VatsimRadarListStatus, StringComparison.Ordinal);
		Assert.False(tab.IsDownloadingVatsimRadarList);
	}

	[Fact]
	public void the_preview_says_whether_the_list_is_included_and_what_fe_buddy_has_of_it()
	{
		(TelephonyViewModel tab, _) = TabUsing(ListOf(("DAL", "Delta", "Fly Delta Virtual")));

		string Row() => tab.BuildPreviewSummary()[0].Rows.Single(row => row.Label == "VATSIM-Radar list").Value;

		Assert.Equal("Not included", Row());

		tab.IncludeVatsimRadarList = true;
		Assert.StartsWith("Included. FE-Buddy's copy is from ", Row(), StringComparison.Ordinal);
	}
}
