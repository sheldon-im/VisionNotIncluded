namespace OniAccess.Widgets {
	/// <summary>
	/// The verbose-UI decoration for one announcement, consumed by
	/// <see cref="WidgetSpeech.Compose(string,string,VerboseMeta)"/>. <see cref="Role"/>
	/// and <see cref="Drillable"/> are spoken before the tooltip; <see cref="Counts"/>
	/// (the "X of Y" / "row X of Y" / "column X of Y" tail) after it. The factories are
	/// the single home for role resolution and count formatting.
	/// </summary>
	public struct VerboseMeta {
		/// <summary>Role tag spoken before the tooltip, or null for no role.</summary>
		public string Role;
		/// <summary>When true, "submenu" is spoken before the tooltip.</summary>
		public bool Drillable;
		/// <summary>Count segments spoken after the tooltip, in order.</summary>
		public string[] Counts;

		public static readonly VerboseMeta None = default;

		/// <summary>
		/// Position-only metadata for a flat list entry (no role). Suppressed when the
		/// position is invalid or the list holds a single item ("1 of 1" is noise).
		/// </summary>
		public static VerboseMeta Position(int position, int total) {
			if (position < 1 || total <= 1) return None;
			return new VerboseMeta { Counts = new[] { PositionText(position, total) } };
		}

		/// <summary>Role, drillability, and position derived from a structured item and its context.</summary>
		public static VerboseMeta ForItem(NavItem item, NavContext ctx) {
			var meta = new VerboseMeta {
				Role = RoleTag(item),
				Drillable = ctx.Drillable,
			};
			if (ctx.Position >= 1 && ctx.Total > 1)
				meta.Counts = new[] { PositionText(ctx.Position, ctx.Total) };
			return meta;
		}

		/// <summary>
		/// A table data cell. The row count is included only when <paramref name="includeRow"/>
		/// (the row changed or context was forced) and the column count only when
		/// <paramref name="includeCol"/>, so the counts follow the same dedupe as the row
		/// label and column name rather than both re-speaking on every move.
		/// </summary>
		public static VerboseMeta DataCell(
				bool includeRow, int rowPos, int rowTotal, bool includeCol, int colPos, int colTotal) {
			var counts = new System.Collections.Generic.List<string>(2);
			if (includeRow)
				counts.Add(string.Format((string)STRINGS.ONIACCESS.VERBOSE.ROW_OF, rowPos, rowTotal));
			if (includeCol)
				counts.Add(string.Format((string)STRINGS.ONIACCESS.VERBOSE.COLUMN_OF, colPos, colTotal));
			return counts.Count == 0 ? None : new VerboseMeta { Counts = counts.ToArray() };
		}

		/// <summary>A table header cell: a sort affordance (when the column sorts) and the column count.</summary>
		public static VerboseMeta HeaderCell(bool sortable, int colPos, int colTotal) {
			return new VerboseMeta {
				Role = sortable ? (string)STRINGS.ONIACCESS.VERBOSE.SORT_BUTTON : null,
				Counts = new[] {
					string.Format((string)STRINGS.ONIACCESS.VERBOSE.COLUMN_OF, colPos, colTotal),
				}
			};
		}

		/// <summary>
		/// Resolve a control's screen-reader role from its <see cref="NavItem.RoleKey"/>.
		/// Read-only rows (null RoleKey) and flat list entries get no tag. The button tag
		/// is gated on <see cref="NavItem.IsActivatable"/> so a no-op row carrying the
		/// button role stays silent. ONI dropdowns are left/right pickers, never combo boxes.
		/// </summary>
		public static string RoleTag(NavItem item) => RoleTag(item.RoleKey, item.IsActivatable());

		/// <summary>Resolve a role word from a role key directly (for hand-built rows that aren't a NavItem).</summary>
		public static string RoleTag(string roleKey, bool activatable) {
			switch (roleKey) {
				case NavRoles.Button:
					return activatable ? (string)STRINGS.ONIACCESS.VERBOSE.ROLE_BUTTON : null;
				case NavRoles.Dropdown:
					return (string)STRINGS.ONIACCESS.VERBOSE.ROLE_PICKER;
				case NavRoles.Slider:
					return (string)STRINGS.ONIACCESS.VERBOSE.ROLE_SLIDER;
				case NavRoles.Toggle:
					return (string)STRINGS.ONIACCESS.VERBOSE.ROLE_TOGGLE;
				case NavRoles.Radio:
					return (string)STRINGS.ONIACCESS.VERBOSE.ROLE_RADIO;
				default:
					return null;
			}
		}

		private static string PositionText(int position, int total) {
			return string.Format((string)STRINGS.ONIACCESS.VERBOSE.POSITION, position, total);
		}
	}
}
