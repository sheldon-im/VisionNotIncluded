using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OniAccess.Speech {
	/// <summary>
	/// Line-by-line review of the current navigation focus (Alt+Up / Alt+Down).
	///
	/// Some controls pack a lot into a single announcement -- the Vitals morale
	/// cell, a building's status panel, a long tooltip. Stepping through that
	/// announcement one line at a time lets the user parse it without re-reading
	/// the whole blob. This is the ONI analogue of the Civ V mod's section review.
	///
	/// The reviewer pulls the focused item's announcement live from the active
	/// handler (see BaseScreenHandler.GetReviewContent) every time the user steps.
	/// Nothing is cached: re-pulling means a value that has since changed is read
	/// fresh, in keeping with the never-cache-game-state rule, and framing the user
	/// never navigated to (the screen name, an incidental notification) is never the
	/// review target because it was never the focus.
	///
	/// Splitting is on the announcement string the handler hands back. Tooltip text
	/// has already had its newlines turned into ". " sentence boundaries upstream
	/// (WidgetOps.CleanTooltipEntry), and the mod's own announcements join their
	/// parts with ". ", so the sentence split below recovers those lines. Each
	/// fragment is filtered again (idempotent) before it is spoken.
	/// </summary>
	public static class LineReview {
		// Split on hard line breaks and on sentence terminators followed by
		// whitespace. The lookbehind keeps the terminator attached to its line.
		private static readonly Regex SplitRegex =
			new Regex(@"\r\n|\r|\n|(?<=[.!?])\s+", RegexOptions.Compiled);

		// Identity of the focus the cursor is currently walking. The cursor rewinds
		// only when this changes (the user moved to a different item), NOT when the
		// content string changes -- a focus whose value ticks (a status panel's live
		// temperature, a morale total) rewrites its announcement every press, and
		// resetting on that would trap the user on the first line. See GetReviewFocusKey.
		private static object _focusKey;
		// The announcement string the current line list was split from. Re-split when
		// it changes so live values read fresh, but without disturbing the cursor.
		private static string _source;
		private static List<string> _lines = new List<string>();
		// -1 means "before the first line": the next step in either direction
		// lands on line 0.
		private static int _cursor = -1;

		/// <summary>Reset state for test isolation.</summary>
		internal static void Reset() {
			_focusKey = null;
			_source = null;
			_lines = new List<string>();
			_cursor = -1;
		}

		/// <summary>
		/// Step through the focused item's announcement. <paramref name="content"/>
		/// is the live announcement from the active handler (null/empty when nothing
		/// is focused); <paramref name="focusKey"/> identifies which item that is, so
		/// the cursor rewinds on a move but not on a live value change;
		/// <paramref name="direction"/> is +1 for next, -1 for previous. Clamps at both
		/// ends; from the fresh state either direction enters at the first line. Speaks
		/// a short notice when there is nothing to review.
		/// </summary>
		public static void Step(string content, object focusKey, int direction) {
			if (string.IsNullOrEmpty(content)) {
				SpeechPipeline.SpeakInterrupt((string)STRINGS.ONIACCESS.HELP.NOTHING_TO_REVIEW);
				return;
			}

			if (!object.Equals(focusKey, _focusKey)) {
				_focusKey = focusKey;
				_cursor = -1;
			}

			if (content != _source) {
				_source = content;
				_lines = Segment(content);
			}

			if (_lines.Count == 0) {
				SpeechPipeline.SpeakInterrupt((string)STRINGS.ONIACCESS.HELP.NOTHING_TO_REVIEW);
				return;
			}

			if (_cursor < 0)
				_cursor = 0;
			else
				_cursor = System.Math.Min(System.Math.Max(_cursor + direction, 0), _lines.Count - 1);

			SpeechPipeline.SpeakInterrupt(_lines[_cursor]);
		}

		/// <summary>
		/// Split an announcement into spoken lines: break on line and sentence
		/// boundaries, filter each fragment, and drop any that filter to nothing.
		/// Exposed for tests.
		/// </summary>
		internal static List<string> Segment(string content) {
			var lines = new List<string>();
			if (string.IsNullOrEmpty(content)) return lines;
			foreach (var fragment in SplitRegex.Split(content)) {
				string filtered = TextFilter.FilterForSpeech(fragment);
				if (!string.IsNullOrEmpty(filtered))
					lines.Add(filtered);
			}
			return lines;
		}
	}
}
