using System.Collections.Generic;

using OniAccess.Navigation;
using OniAccess.Speech;
using OniAccess.Widgets;

namespace OniAccess.Handlers.Screens.Starmap {
	/// <summary>
	/// Tab 1: Rockets. Three-level tree.
	/// Level 0 = rocket list (search level).
	/// Level 1 = detail categories (Status, Checklist, Range, etc.).
	/// Level 2 = items within category.
	/// Space launches the active rocket from any level.
	/// </summary>
	internal class RocketsTab: NavTreeHandler, IScreenTab {
		private readonly StarmapScreenHandler _parent;

		internal RocketsTab(StarmapScreenHandler parent) : base(screen: null) {
			_parent = parent;
		}

		public string TabName => (string)STRINGS.ONIACCESS.STARMAP.ROCKETS_TAB;

		public override string DisplayName => TabName;

		// Type-ahead targets the rocket list (level 0), even while drilled into a
		// rocket's detail categories.
		protected override SearchScope SearchScope => SearchScope.Roots;

		private static readonly List<HelpEntry> _helpEntries = new List<HelpEntry>(DrillNavHelpEntries) {
			new HelpEntry("Space", STRINGS.ONIACCESS.STARMAP.LAUNCH_HELP),
		};

		public override IReadOnlyList<HelpEntry> HelpEntries => _helpEntries;

		// ========================================
		// IScreenTab
		// ========================================

		public void OnTabActivated(bool announce) {
			ResetState();
			if (announce)
				SpeechPipeline.SpeakInterrupt(TabName);
			if (ItemCount > 0) {
				var rockets = StarmapHelper.GetSpacecraft();
				int i = Nav.Path[0];
				if (i >= 0 && i < rockets.Count)
					_parent.SetActiveRocket(rockets[i]);
				AnnounceCurrent(interrupt: false);
			} else {
				SpeechPipeline.SpeakQueued(STRINGS.ONIACCESS.STARMAP.NO_ROCKETS);
			}
		}

		public void OnTabDeactivated() {
			_search.Clear();
		}

		public bool HandleInput() {
			if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Space)) {
				var rocket = _parent.ActiveRocket;
				string result = StarmapHelper.TryLaunch(rocket);
				SpeechPipeline.SpeakInterrupt(result);
				return true;
			}
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
			foreach (var rocket in StarmapHelper.GetSpacecraft()) {
				var r = rocket;
				roots.Add(new MenuNode(
					() => StarmapHelper.BuildRocketListLabel(r),
					children: () => BuildCategories(r)));
			}
			return roots;
		}

		private IReadOnlyList<NavItem> BuildCategories(Spacecraft rocket) {
			var categories = StarmapHelper.BuildRocketCategories(rocket);
			var list = new List<NavItem>(categories.Count);
			foreach (var category in categories) {
				var cat = category;
				list.Add(new MenuNode(
					() => cat.Name,
					children: () => BuildItems(cat)));
			}
			return list;
		}

		private static IReadOnlyList<NavItem> BuildItems(StarmapHelper.RocketCategory category) {
			var list = new List<NavItem>(category.Items.Count);
			foreach (var item in category.Items) {
				var text = item;
				list.Add(new MenuNode(() => text));
			}
			return list;
		}

		// Selecting a rocket makes it active before drilling into its details.
		protected override void ActivateCurrentItem() {
			if (Nav.Depth == 0) {
				var rockets = StarmapHelper.GetSpacecraft();
				int i = Nav.Path[0];
				if (i >= 0 && i < rockets.Count)
					_parent.SetActiveRocket(rockets[i]);
			}
			base.ActivateCurrentItem();
		}
	}
}
