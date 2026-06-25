namespace OniAccess.Handlers.Screens {
	/// <summary>
	/// Opt-in for tabs that are not themselves handlers (so they don't inherit
	/// BaseScreenHandler.GetReviewContent) but still want Alt+Up/Down line review of
	/// their focused element. TabbedScreenHandler pulls this when the active tab is
	/// not a handler. Most tabs are menu or tree handlers and never need it.
	/// </summary>
	public interface IReviewableTab {
		/// <summary>
		/// The focused element rendered as an announcement string for the line
		/// reviewer, or null when nothing is focused. Same text the user last heard.
		/// </summary>
		string GetReviewContent();

		/// <summary>
		/// Identity of the focused element, so the reviewer rewinds on a move but not
		/// on a live value change. Must stay stable while the same element is focused.
		/// </summary>
		object GetReviewFocusKey();
	}
}
