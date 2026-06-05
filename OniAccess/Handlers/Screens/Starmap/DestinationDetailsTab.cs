using System.Collections.Generic;

using OniAccess.Navigation;
using OniAccess.Speech;
using OniAccess.Widgets;

namespace OniAccess.Handlers.Screens.Starmap {
	/// <summary>
	/// Tab 3: Destination Details. Two-level tree.
	/// Level 0 = sections (identity, analysis, research, mass,
	///           composition, resources, artifacts) plus analyze action.
	/// Level 1 = items within section.
	/// The analyze action is a leaf at level 0 (empty Items). Enter on any leaf
	/// toggles analysis of the selected destination, matching the old handler.
	/// </summary>
	internal class DestinationDetailsTab: NavTreeHandler, IScreenTab {
		private readonly StarmapScreenHandler _parent;

		internal DestinationDetailsTab(StarmapScreenHandler parent)
				: base(screen: null) {
			_parent = parent;
		}

		public string TabName =>
			(string)STRINGS.ONIACCESS.STARMAP.DETAILS_TAB;

		public override string DisplayName => TabName;

		// Type-ahead targets the section list (level 0).
		protected override SearchScope SearchScope => SearchScope.Roots;

		public override IReadOnlyList<HelpEntry> HelpEntries => DrillNavHelpEntries;

		// ========================================
		// IScreenTab
		// ========================================

		public void OnTabActivated(bool announce) {
			ResetState();
			if (announce)
				SpeechPipeline.SpeakInterrupt(TabName);
			if (ItemCount > 0)
				AnnounceCurrent(interrupt: false);
			else
				SpeechPipeline.SpeakQueued(
					STRINGS.ONIACCESS.STARMAP.NO_DESTINATION_SELECTED);
		}

		public void OnTabDeactivated() {
			_search.Clear();
		}

		public bool HandleInput() {
			return base.Tick();
		}

		public new bool HandleKeyDown(KButtonEvent e) {
			return base.HandleKeyDown(e);
		}

		internal void OnDestinationChanged() {
			ResetState();
		}

		// ========================================
		// TREE CONSTRUCTION
		// ========================================

		protected override IReadOnlyList<NavItem> BuildRoots() {
			var dest = _parent.SelectedDestination;
			if (dest == null) return new List<NavItem>();

			var sections = StarmapHelper.BuildDestinationSections(dest, _parent.ActiveRocket);
			var roots = new List<NavItem>(sections.Count);
			for (int s = 0; s < sections.Count; s++) {
				var sec = sections[s];
				bool isAnalyze = s == sections.Count - 1;
				roots.Add(new MenuNode(
					announce: isAnalyze
						? (System.Func<string>)(() =>
							StarmapHelper.GetAnalyzeActionLabel(_parent.SelectedDestination))
						: (() => sec.Name),
					children: () => BuildItems(sec),
					activate: () => { ActivateAnalyze(); return true; }));
			}
			return roots;
		}

		private IReadOnlyList<NavItem> BuildItems(StarmapHelper.DestinationSection section) {
			var list = new List<NavItem>(section.Items.Count);
			foreach (var item in section.Items) {
				var text = item;
				// Enter on any leaf toggles analysis, as the old handler did.
				list.Add(new MenuNode(
					() => text,
					activate: () => { ActivateAnalyze(); return true; }));
			}
			return list;
		}

		private void ActivateAnalyze() {
			var dest = _parent.SelectedDestination;
			if (dest == null) return;

			if (StarmapHelper.IsAnalyzed(dest)) {
				SpeechPipeline.SpeakInterrupt(
					(string)STRINGS.UI.STARMAP.ANALYSIS_COMPLETE);
				return;
			}

			int currentTarget = SpacecraftManager.instance
				.GetStarmapAnalysisDestinationID();
			if (currentTarget == dest.id) {
				SpacecraftManager.instance.SetStarmapAnalysisDestinationID(-1);
				StarmapHelper.PlaySound("HUD_Click");
				SpeechPipeline.SpeakInterrupt(
					STRINGS.ONIACCESS.STARMAP.ANALYSIS_SUSPENDED);
			} else {
				SpacecraftManager.instance
					.SetStarmapAnalysisDestinationID(dest.id);
				StarmapHelper.PlaySound("HUD_Click");
				SpeechPipeline.SpeakInterrupt(
					STRINGS.ONIACCESS.STARMAP.ANALYSIS_STARTED);
			}
		}
	}
}
