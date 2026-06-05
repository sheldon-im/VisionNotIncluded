using System.Collections.Generic;

using OniAccess.Navigation;
using OniAccess.Speech;
using OniAccess.Widgets;

namespace OniAccess.Handlers.Screens.Research {
	/// <summary>
	/// Browse tab: two-level tree.
	/// Level 0 = buckets (Available, Locked, Completed).
	/// Level 1 = techs within the selected bucket.
	/// Type-ahead searches all techs by name and what they unlock.
	/// Enter queues research. Space jumps to Tree tab.
	/// </summary>
	internal class BrowseTab: NavTreeHandler, IScreenTab {
		private readonly ResearchScreenHandler _parent;

		internal BrowseTab(ResearchScreenHandler parent) : base(screen: null) {
			_parent = parent;
		}

		public string TabName => (string)STRINGS.ONIACCESS.RESEARCH.BROWSE_TAB;

		public override string DisplayName => TabName;

		protected override int StartDepth => 1;

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
		}

		public void OnTabDeactivated() {
			_search.Clear();
		}

		public bool HandleInput() {
			if (UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.Space) && Nav.Depth == 1) {
				var techs = ResearchHelper.GetTechsInBucket(Nav.Path[0]);
				int idx = Nav.Path[1];
				if (idx >= 0 && idx < techs.Count) {
					_parent.JumpToTreeTab(techs[idx]);
					return true;
				}
			}
			// base.Tick() handles Up/Down/Home/End/Enter/Left/Right/Search.
			// Tab was already consumed by the parent so GetKeyDown(Tab) is false.
			return base.Tick();
		}

		public new bool HandleKeyDown(KButtonEvent e) {
			return base.HandleKeyDown(e);
		}

		// ========================================
		// TREE CONSTRUCTION
		// ========================================

		protected override IReadOnlyList<NavItem> BuildRoots() {
			var roots = new List<NavItem>(3);
			for (int b = 0; b < 3; b++) {
				int bucket = b;
				roots.Add(new MenuNode(
					() => ResearchHelper.GetBucketName(bucket),
					children: () => BuildTechs(bucket)));
			}
			return roots;
		}

		private IReadOnlyList<NavItem> BuildTechs(int bucket) {
			var techs = ResearchHelper.GetTechsInBucket(bucket);
			var list = new List<NavItem>(techs.Count);
			foreach (var t in techs) {
				var tech = t;
				list.Add(new MenuNode(
					() => ResearchHelper.BuildTechLabel(tech),
					activate: () => { QueueResearch(tech); return true; },
					searchText: () => ResearchHelper.BuildSearchLabel(tech)));
			}
			return list;
		}

		private void QueueResearch(Tech tech) {
			if (tech.IsComplete()) {
				ResearchHelper.PlayRejectSound();
				SpeechPipeline.SpeakInterrupt(
					tech.Name + ", " + STRINGS.ONIACCESS.RESEARCH.COMPLETED);
				return;
			}

			ResearchHelper.PlayClickSound();
			global::Research.Instance.SetActiveResearch(tech, clearQueue: true);
			SpeechPipeline.SpeakInterrupt(
				string.Format(STRINGS.ONIACCESS.RESEARCH.QUEUED, tech.Name));
		}
	}
}
