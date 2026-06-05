using System.Collections.Generic;

using OniAccess.Navigation;
using OniAccess.Speech;
using OniAccess.Widgets;

namespace OniAccess.Handlers.Screens.Starmap {
	/// <summary>
	/// Tab 2: Destinations. Two-level tree.
	/// Level 0 = pre-filtered non-empty distance tiers.
	/// Level 1 = destinations within the tier.
	/// Enter selects destination, assigns to active rocket if grounded,
	/// and auto-switches to Tab 3 (destination details).
	/// </summary>
	internal class DestinationsTab: NavTreeHandler, IScreenTab {
		private readonly StarmapScreenHandler _parent;

		internal DestinationsTab(StarmapScreenHandler parent) : base(screen: null) {
			_parent = parent;
		}

		public string TabName {
			get {
				string name = (string)STRINGS.ONIACCESS.STARMAP.DESTINATIONS_TAB;
				var rocket = _parent.ActiveRocket;
				if (rocket != null
						&& rocket.state == Spacecraft.MissionState.Grounded)
					return string.Format(
						STRINGS.ONIACCESS.STARMAP.DESTINATIONS_TAB_WITH_ROCKET,
						rocket.GetRocketName());
				return name;
			}
		}

		public override string DisplayName => TabName;

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
				SpeechPipeline.SpeakQueued(STRINGS.ONIACCESS.STARMAP.NO_DESTINATIONS);
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

		// ========================================
		// TREE CONSTRUCTION
		// ========================================

		protected override IReadOnlyList<NavItem> BuildRoots() {
			var roots = new List<NavItem>();
			foreach (var tier in StarmapHelper.GetPopulatedDistanceTiers()) {
				int t = tier;
				roots.Add(new MenuNode(
					() => StarmapHelper.GetTierLabel(t),
					children: () => BuildDestinations(t)));
			}
			return roots;
		}

		private IReadOnlyList<NavItem> BuildDestinations(int tier) {
			var dests = StarmapHelper.GetDestinationsAtTier(tier);
			var list = new List<NavItem>(dests.Count);
			foreach (var dest in dests) {
				var d = dest;
				list.Add(new MenuNode(
					() => StarmapHelper.GetDestinationLabel(d),
					activate: () => { ActivateDestination(d); return true; }));
			}
			return list;
		}

		private void ActivateDestination(SpaceDestination dest) {
			_parent.SelectDestination(dest);

			string destName = StarmapHelper.IsAnalyzed(dest)
				? dest.GetDestinationType().Name
				: (string)STRINGS.UI.STARMAP.UNKNOWN_DESTINATION;

			// Assign to active rocket if grounded
			var rocket = _parent.ActiveRocket;
			if (rocket != null
					&& rocket.state == Spacecraft.MissionState.Grounded) {
				SpacecraftManager.instance.SetSpacecraftDestination(
					rocket.launchConditions, dest);
				SpeechPipeline.SpeakInterrupt(string.Format(
					STRINGS.ONIACCESS.STARMAP.DESTINATION_ASSIGNED,
					destName, rocket.GetRocketName()));
			} else {
				SpeechPipeline.SpeakInterrupt(destName);
			}

			// Auto-switch to destination details tab
			_parent.JumpToDetailsTab();
		}
	}
}
