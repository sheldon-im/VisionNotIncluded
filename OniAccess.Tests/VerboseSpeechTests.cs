using System;
using System.Collections.Generic;

using OniAccess;
using OniAccess.Widgets;

namespace OniAccess.Tests {
	/// <summary>
	/// Offline tests for the single verbose-UI composer (WidgetSpeech + VerboseMeta).
	/// They pin the spoken assembly order so the verbose result stays faithful: body,
	/// role, "submenu", tooltip, then the count tail. With verbose off the output must
	/// be the undecorated body, byte for byte.
	/// </summary>
	static class VerboseSpeechTests {
		// Configurable NavItem so a single helper can pose as any control role.
		private class FakeItem: NavItem {
			public string Text = "Label";
			public string Role;
			public bool Activatable;

			public string RoleKey => Role;
			public bool IsNavigable() => true;
			public bool IsActivatable() => Activatable;
			public string Announce() => Text;
			public string SearchText => Text;
			public string ContextLabel => Text;
			public bool Activate() => false;
			public bool Adjust(int direction, int stepLevel) => false;
			public IReadOnlyList<NavItem> GetChildren() => Array.Empty<NavItem>();
		}

		private static (string, bool, string) Check(string name, bool ok, string detail)
			=> (name, ok, ok ? "OK" : detail);

		// The composer reads Verbosity.IsOn, which reads ConfigManager.Config. Seed a
		// config object once via the (private) setter so per-test toggling is a plain
		// field write with no file IO.
		private static void SetVerbose(bool on) {
			if (ConfigManager.Config == null) {
				var prop = typeof(ConfigManager).GetProperty("Config");
				prop.GetSetMethod(true).Invoke(null, new object[] { new ModConfig() });
			}
			ConfigManager.Config.VerboseUi = on;
		}

		// ========================================
		// Structured-item path (role + submenu + position)
		// ========================================

		private static (string, bool, string) VerboseOffIsUndecorated() {
			SetVerbose(false);
			var item = new FakeItem { Role = NavRoles.Button, Activatable = true };
			string r = WidgetSpeech.Compose(item, new NavContext { Position = 2, Total = 5 }, null);
			return Check("VerboseOffIsUndecorated", r == "Label", $"got \"{r}\"");
		}

		private static (string, bool, string) RoleThenPosition() {
			SetVerbose(true);
			var item = new FakeItem { Role = NavRoles.Button, Activatable = true };
			string r = WidgetSpeech.Compose(item, new NavContext { Position = 2, Total = 5 }, null);
			return Check("RoleThenPosition", r == "Label, button, 2 of 5", $"got \"{r}\"");
		}

		private static (string, bool, string) ButtonRoleGatedOnActivatable() {
			SetVerbose(true);
			var item = new FakeItem { Role = NavRoles.Button, Activatable = false };
			string r = WidgetSpeech.Compose(item, new NavContext { Position = 2, Total = 5 }, null);
			return Check("ButtonRoleGatedOnActivatable", r == "Label, 2 of 5", $"got \"{r}\"");
		}

		private static (string, bool, string) DropdownIsPickerWithSubmenu() {
			SetVerbose(true);
			var item = new FakeItem { Role = NavRoles.Dropdown };
			string r = WidgetSpeech.Compose(item,
				new NavContext { Position = 1, Total = 3, Drillable = true }, null);
			return Check("DropdownIsPickerWithSubmenu", r == "Label, picker, submenu, 1 of 3", $"got \"{r}\"");
		}

		private static (string, bool, string) ToggleRoleSpokenAsToggle() {
			SetVerbose(true);
			var item = new FakeItem { Role = NavRoles.Toggle };
			string r = WidgetSpeech.Compose(item, new NavContext { Position = 1, Total = 2 }, null);
			return Check("ToggleRoleSpokenAsToggle", r == "Label, toggle, 1 of 2", $"got \"{r}\"");
		}

		private static (string, bool, string) PositionSuppressedWhenInvalid() {
			SetVerbose(true);
			var item = new FakeItem { Role = null };
			string r = WidgetSpeech.Compose(item, new NavContext { Position = -1, Total = -1 }, null);
			return Check("PositionSuppressedWhenInvalid", r == "Label", $"got \"{r}\"");
		}

		private static (string, bool, string) PositionIsLastAfterTooltip() {
			SetVerbose(true);
			var item = new FakeItem { Role = NavRoles.Button, Activatable = true };
			string r = WidgetSpeech.Compose(item, new NavContext { Position = 2, Total = 5 }, "details");
			bool ok = r.EndsWith("2 of 5")
				&& r.IndexOf("button", StringComparison.Ordinal) >= 0
				&& r.IndexOf("details", StringComparison.Ordinal) > r.IndexOf("button", StringComparison.Ordinal)
				&& r.IndexOf("2 of 5", StringComparison.Ordinal) > r.IndexOf("details", StringComparison.Ordinal);
			return Check("PositionIsLastAfterTooltip", ok, $"got \"{r}\"");
		}

		private static (string, bool, string) ReviewJoinsTooltipFieldsWithPeriods() {
			SetVerbose(true);
			var item = new FakeItem { Text = "Endurance: 75%" };
			string r = WidgetSpeech.ComposeReview(item,
				new NavContext { Position = 5, Total = 10 },
				"Standard Duplicant: -70%/cycle. Barracks: 100%/cycle");
			// The tooltip fields and the position tail join with ". " (not the spoken
			// ", ") so the line reviewer splits each onto its own line. Without this the
			// whole readout collapses into one un-steppable line.
			bool ok = r == "Endurance: 75%. Standard Duplicant: -70%/cycle. Barracks: 100%/cycle. 5 of 10";
			return Check("ReviewJoinsTooltipFieldsWithPeriods", ok, $"got \"{r}\"");
		}

		// ========================================
		// Flat-list path (position only, never a role)
		// ========================================

		private static (string, bool, string) ListItemPositionOnly() {
			SetVerbose(true);
			string r = WidgetSpeech.ComposeListItem("Item", 3, 7);
			return Check("ListItemPositionOnly", r == "Item, 3 of 7", $"got \"{r}\"");
		}

		private static (string, bool, string) ListItemSuppressedWhenZero() {
			SetVerbose(true);
			string r = WidgetSpeech.ComposeListItem("Item", 0, 7);
			return Check("ListItemSuppressedWhenZero", r == "Item", $"got \"{r}\"");
		}

		private static (string, bool, string) ListItemSuppressedWhenSingle() {
			SetVerbose(true);
			string r = WidgetSpeech.ComposeListItem("Item", 1, 1);
			return Check("ListItemSuppressedWhenSingle", r == "Item", $"got \"{r}\"");
		}

		// ========================================
		// Table path (row/column counts, sort affordance)
		// ========================================

		private static (string, bool, string) DataCellRowThenColumn() {
			SetVerbose(true);
			string r = WidgetSpeech.Compose("Cell", null, VerboseMeta.DataCell(true, 2, 5, true, 3, 6));
			return Check("DataCellRowThenColumn", r == "Cell, row 2 of 5, column 3 of 6", $"got \"{r}\"");
		}

		private static (string, bool, string) DataCellRowCountOnlyWhenColumnUnchanged() {
			SetVerbose(true);
			string r = WidgetSpeech.Compose("Cell", null, VerboseMeta.DataCell(true, 2, 5, false, 3, 6));
			return Check("DataCellRowCountOnlyWhenColumnUnchanged", r == "Cell, row 2 of 5", $"got \"{r}\"");
		}

		private static (string, bool, string) DataCellColumnCountOnlyWhenRowUnchanged() {
			SetVerbose(true);
			string r = WidgetSpeech.Compose("Cell", null, VerboseMeta.DataCell(false, 2, 5, true, 3, 6));
			return Check("DataCellColumnCountOnlyWhenRowUnchanged", r == "Cell, column 3 of 6", $"got \"{r}\"");
		}

		private static (string, bool, string) HeaderCellSortable() {
			SetVerbose(true);
			string r = WidgetSpeech.Compose("Name", null, VerboseMeta.HeaderCell(true, 1, 4));
			return Check("HeaderCellSortable", r == "Name, sort button, column 1 of 4", $"got \"{r}\"");
		}

		private static (string, bool, string) HeaderCellNotSortableHasNoAffordance() {
			SetVerbose(true);
			string r = WidgetSpeech.Compose("Name", null, VerboseMeta.HeaderCell(false, 2, 4));
			return Check("HeaderCellNotSortableHasNoAffordance", r == "Name, column 2 of 4", $"got \"{r}\"");
		}

		private static (string, bool, string) TableCellUndecoratedWhenOff() {
			SetVerbose(false);
			string r = WidgetSpeech.Compose("Cell", null, VerboseMeta.DataCell(true, 2, 5, true, 3, 6));
			return Check("TableCellUndecoratedWhenOff", r == "Cell", $"got \"{r}\"");
		}

		private static (string, bool, string) KindSuffixAppended() {
			SetVerbose(true);
			string r = Verbosity.WithKindSuffix("Schedules", "grid");
			return Check("KindSuffixAppended", r == "Schedules, grid", $"got \"{r}\"");
		}

		private static (string, bool, string) KindSuffixNotDoubledWhenBakedIn() {
			SetVerbose(true);
			string r = Verbosity.WithKindSuffix("Priorities table", "table");
			return Check("KindSuffixNotDoubledWhenBakedIn", r == "Priorities table", $"got \"{r}\"");
		}

		private static (string, bool, string) KindSuffixUnchangedWhenOff() {
			SetVerbose(false);
			string r = Verbosity.WithKindSuffix("Schedules", "grid");
			return Check("KindSuffixUnchangedWhenOff", r == "Schedules", $"got \"{r}\"");
		}

		public static IEnumerable<(string, bool, string)> All() {
			yield return VerboseOffIsUndecorated();
			yield return RoleThenPosition();
			yield return ButtonRoleGatedOnActivatable();
			yield return DropdownIsPickerWithSubmenu();
			yield return ToggleRoleSpokenAsToggle();
			yield return PositionSuppressedWhenInvalid();
			yield return PositionIsLastAfterTooltip();
			yield return ReviewJoinsTooltipFieldsWithPeriods();
			yield return ListItemPositionOnly();
			yield return ListItemSuppressedWhenZero();
			yield return ListItemSuppressedWhenSingle();
			yield return DataCellRowThenColumn();
			yield return DataCellRowCountOnlyWhenColumnUnchanged();
			yield return DataCellColumnCountOnlyWhenRowUnchanged();
			yield return HeaderCellSortable();
			yield return HeaderCellNotSortableHasNoAffordance();
			yield return TableCellUndecoratedWhenOff();
			yield return KindSuffixAppended();
			yield return KindSuffixNotDoubledWhenBakedIn();
			yield return KindSuffixUnchangedWhenOff();
			// Leave verbose off so later suites see the shipped default.
			SetVerbose(false);
		}
	}
}
