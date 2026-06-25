using OniAccess.Input;

namespace OniAccess.Handlers.Screens {
	/// <summary>
	/// Base class for screen handlers that compose multiple IScreenTab objects
	/// and cycle between them with Tab/Shift+Tab. Each tab is an autonomous
	/// object that owns its own input handling and announcements.
	///
	/// Not all handlers with Tab cycling use this. Handlers that cycle an index
	/// over their own content (DetailsScreen sections, ReportScreen days,
	/// MinionSelect slots, etc.) don't have composed tab objects and extend
	/// their navigation base classes directly.
	///
	/// Provides tab array management, CycleTab with wrap detection, and default
	/// Tick/HandleKeyDown/OnDeactivate that delegate to the active tab.
	///
	/// Subclasses set _tabs in their constructor and override OnActivate to
	/// select the initial tab. Override HandleTabKey for custom Tab behavior
	/// (e.g., CodexScreenHandler) and HandleKeyDown for custom Escape handling.
	/// </summary>
	public abstract class TabbedScreenHandler: BaseScreenHandler {
		private IScreenTab[] _tabArray;
		private int _activeTabIndex;

		private static readonly HelpEntry _tabSwitchHelp =
			new HelpEntry("Tab/Shift+Tab", STRINGS.ONIACCESS.HELP.SWITCH_PANEL);

		protected TabbedScreenHandler(KScreen screen) : base(screen) { }

		/// <summary>
		/// Returns the active tab's help entries plus Tab/Shift+Tab.
		/// Subclasses can override to add screen-level entries.
		/// </summary>
		public override System.Collections.Generic.IReadOnlyList<HelpEntry> HelpEntries {
			get {
				var tabEntries = _tabArray[_activeTabIndex].HelpEntries;
				if (tabEntries == null)
					return new System.Collections.Generic.List<HelpEntry> { _tabSwitchHelp, LineReviewHelpEntry };
				var entries = new System.Collections.Generic.List<HelpEntry>(tabEntries);
				entries.Add(_tabSwitchHelp);
				entries.Add(LineReviewHelpEntry);
				return entries;
			}
		}

		/// <summary>
		/// Set by subclass constructors after creating tab objects.
		/// </summary>
		protected void SetTabs(params IScreenTab[] tabs) {
			_tabArray = tabs;
		}

		protected int ActiveTabIndex {
			get => _activeTabIndex;
			set => _activeTabIndex = value;
		}

		/// <summary>
		/// Deactivate the current tab before switching to a new one.
		/// Used by subclass jump methods (e.g., JumpToTreeTab).
		/// </summary>
		protected void DeactivateCurrentTab() {
			_tabArray[_activeTabIndex].OnTabDeactivated();
		}

		/// <summary>
		/// Activate the current tab after switching ActiveTabIndex.
		/// Used by subclass jump methods after setting the new index.
		/// </summary>
		protected void ActivateCurrentTab(bool announce) {
			_tabArray[_activeTabIndex].OnTabActivated(announce);
			if (announce) AnnounceTabPosition();
		}

		/// <summary>
		/// Queue "tab X of Y" after the active tab's own announcement so a verbose
		/// tab switch ends with the tab's position. Queued (not interrupt) so it
		/// trails the tab name and the landed content the tab just spoke.
		/// </summary>
		private void AnnounceTabPosition() {
			Verbosity.SpeakTabPosition(_activeTabIndex + 1, _tabArray.Length);
		}

		public override void OnDeactivate() {
			_tabArray[_activeTabIndex].OnTabDeactivated();
			base.OnDeactivate();
		}

		/// <summary>
		/// Pull review content from the active tab. Nearly every tab is itself a menu
		/// or tree handler, so it already supplies its focused item; a tab that is not
		/// a handler (or has nothing focused) yields null and the reviewer says so.
		/// </summary>
		internal override string GetReviewContent() {
			var tab = _tabArray[_activeTabIndex];
			if (tab is BaseScreenHandler h) return h.GetReviewContent();
			if (tab is IReviewableTab r) return r.GetReviewContent();
			return null;
		}

		// Fold the active tab index into the key so switching tabs rewinds the
		// reviewer, then defer to the tab for its own focus identity.
		internal override object GetReviewFocusKey() {
			var tab = _tabArray[_activeTabIndex];
			object inner = tab is BaseScreenHandler h ? h.GetReviewFocusKey()
				: tab is IReviewableTab r ? r.GetReviewFocusKey()
				: null;
			return (_activeTabIndex, inner);
		}

		public override bool Tick() {
			if (base.Tick()) return true;

			if (TryLineReview())
				return true;

			if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Tab)) {
				return HandleTabKey();
			}

			return _tabArray[_activeTabIndex].HandleInput();
		}

		/// <summary>
		/// Handle the Tab key press. Default cycles tabs via CycleTab.
		/// Override for screens with custom Tab behavior.
		/// </summary>
		protected virtual bool HandleTabKey() {
			int dir = InputUtil.ShiftHeld() ? -1 : 1;
			CycleTab(dir);
			return true;
		}

		public override bool HandleKeyDown(KButtonEvent e) {
			return _tabArray[_activeTabIndex].HandleKeyDown(e);
		}

		protected void CycleTab(int direction) {
			_tabArray[_activeTabIndex].OnTabDeactivated();
			int next = (_activeTabIndex + direction + _tabArray.Length) % _tabArray.Length;
			bool wrapped = direction > 0 ? next <= _activeTabIndex : next >= _activeTabIndex;
			_activeTabIndex = next;
			if (wrapped) PlaySound("HUD_Click");
			else PlaySound("HUD_Mouseover");
			ActivateCurrentTab(announce: true);
		}
	}
}
