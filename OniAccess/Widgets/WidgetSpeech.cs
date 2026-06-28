namespace OniAccess.Widgets {
	/// <summary>
	/// The single composer for spoken item text. Every navigation model routes
	/// per-item speech through here, so cross-cutting decoration (verbose UI: role
	/// tags, "submenu" on drillables, position-within-list, table row/column counts)
	/// is implemented in exactly one place.
	///
	/// Assembly order, matching the established verbose result: body (the item's own
	/// label/value, already including any "unavailable" marker), then the role tag,
	/// then "submenu", then the tooltip, then the count tail. Role/submenu/counts are
	/// spoken only when <see cref="Verbosity.IsOn"/>; with verbose off the output is
	/// byte-for-byte the pre-verbosity speech (body plus tooltip).
	/// </summary>
	public static class WidgetSpeech {
		/// <summary>
		/// Core composer: an already-assembled body plus its tooltip and verbose
		/// metadata. All other overloads funnel here.
		/// </summary>
		public static string Compose(string body, string tooltip, VerboseMeta meta) {
			return Compose(body, tooltip, meta, perLine: false);
		}

		// perLine joins the tooltip fields and the count tail with ". " instead of ", "
		// so the Alt+Up/Down reviewer splits each onto its own line; the spoken path
		// (perLine: false) keeps the flat comma cadence. Role and "submenu" stay attached
		// to the body's line either way.
		private static string Compose(string body, string tooltip, VerboseMeta meta, bool perLine) {
			string text = body;
			if (Verbosity.IsOn) {
				text = Append(text, meta.Role);
				if (meta.Drillable)
					text = Append(text, (string)STRINGS.ONIACCESS.VERBOSE.SUBMENU);
			}
			string sep = perLine ? ". " : ", ";
			// Dedup the tooltip against the undecorated body only, so a tooltip
			// sentence is never dropped just for matching an injected role word.
			text = WidgetOps.AppendTooltip(text, tooltip, body, sep);
			if (Verbosity.IsOn && meta.Counts != null) {
				for (int i = 0; i < meta.Counts.Length; i++)
					text = AppendSep(text, meta.Counts[i], sep);
			}
			return text;
		}

		/// <summary>
		/// Structured item (live widget or menu node): derive the verbose metadata
		/// from the item's role and the navigation context, then compose.
		/// </summary>
		public static string Compose(NavItem item, NavContext ctx, string tooltip) {
			return Compose(item.Announce(), tooltip, VerboseMeta.ForItem(item, ctx));
		}

		/// <summary>
		/// Compose a structured item for the Alt+Up/Down line reviewer: identical to the
		/// spoken <see cref="Compose(NavItem,NavContext,string)"/> except the tooltip
		/// fields and the position tail are joined with ". " instead of ", ", so the
		/// reviewer's splitter breaks each onto its own line. The spoken announcement is
		/// unaffected.
		/// </summary>
		public static string ComposeReview(NavItem item, NavContext ctx, string tooltip) {
			return Compose(item.Announce(), tooltip, VerboseMeta.ForItem(item, ctx), perLine: true);
		}

		/// <summary>
		/// A flat-list entry: an assembled body with a position readout. Pass a 1-based
		/// position; a value below 1 suppresses the count. A generic list selection
		/// passes no role; a hand-built control row (a settings toggle/slider) passes its
		/// role key so it speaks the role like a widget-backed control would.
		/// </summary>
		public static string ComposeListItem(string body, int position, int total, string roleKey = null) {
			var meta = VerboseMeta.Position(position, total);
			meta.Role = VerboseMeta.RoleTag(roleKey, true);
			return Compose(body, null, meta);
		}

		/// <summary>
		/// A context-free status string (toasts, errors, feedback) that carries no
		/// item role or position. Spoken identically with verbose on or off.
		/// </summary>
		public static string ComposeLabel(string text) {
			return Compose(text, null, VerboseMeta.None);
		}

		private static string Append(string text, string segment) => AppendSep(text, segment, ", ");

		private static string AppendSep(string text, string segment, string sep) {
			if (string.IsNullOrEmpty(segment)) return text;
			if (string.IsNullOrEmpty(text)) return segment;
			return text + sep + segment;
		}
	}
}
