namespace OniAccess {
	/// <summary>
	/// The verbose-UI setting. When on, item announcements gain screen-reader-style
	/// metadata (control role, "submenu" on drillables, position-within-list, table
	/// row/column counts, a "table"/"grid" suffix on table names), always appended at
	/// the END of an utterance so the leading distinguishing word is never delayed.
	///
	/// All decoration lives in <see cref="Widgets.WidgetSpeech"/>; this is only the
	/// on/off gate. Backed by the persisted <see cref="ModConfig.VerboseUi"/> flag,
	/// read live so a toggle in the config screen takes effect on the next announce.
	/// </summary>
	public static class Verbosity {
		public static bool IsOn => ConfigManager.Config.VerboseUi;

		/// <summary>
		/// Append a kind suffix ("table", "grid") to a surface name when verbose is on,
		/// e.g. "Vitals, table". Single home for the name-plus-kind format shared by the
		/// table base and the 2D-grid screens that can't inherit it. Skips the append when
		/// the name already ends with the suffix word (some screen names bake it in, e.g.
		/// "Priorities table"), so the kind isn't spoken twice.
		/// </summary>
		public static string WithKindSuffix(string name, string suffix) {
			if (!IsOn || name == null) return name;
			if (EndsWithWord(name, suffix)) return name;
			return name + ", " + suffix;
		}

		private static bool EndsWithWord(string text, string word) {
			if (!text.EndsWith(word, System.StringComparison.OrdinalIgnoreCase)) return false;
			int before = text.Length - word.Length - 1;
			return before < 0 || !char.IsLetter(text[before]);
		}

		/// <summary>
		/// Queue "tab X of Y" at the end of a tab switch when verbose is on. Shared by
		/// the tabbed-screen base and the details screen, which derive position and count
		/// differently but speak the readout identically. Suppressed when verbose is off
		/// or there is only one tab.
		/// </summary>
		public static void SpeakTabPosition(int position, int count) {
			if (!IsOn || count <= 1) return;
			Speech.SpeechPipeline.SpeakQueued(string.Format(
				(string)STRINGS.ONIACCESS.VERBOSE.TAB_OF, position, count));
		}
	}
}
