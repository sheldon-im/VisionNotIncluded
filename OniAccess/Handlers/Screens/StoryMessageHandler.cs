using System.Collections.Generic;
using HarmonyLib;

using OniAccess.Widgets;

namespace OniAccess.Handlers.Screens {
	/// <summary>
	/// Handler for StoryMessageScreen, a blocking popup shown during victory
	/// sequences (ColonyAchievementTracker.BeginVictorySequence). Presents
	/// the achievement title + body as a Label and the dismiss button.
	///
	/// Title and body are set via property setters after StartScreen + Show,
	/// so DeferFirstDiscovery skips the initial call and allows up to 3 retries.
	/// </summary>
	public class StoryMessageHandler: BaseWidgetHandler {
		public override string DisplayName =>
			(string)STRINGS.ONIACCESS.HANDLERS.STORY_MESSAGE;

		public override IReadOnlyList<HelpEntry> HelpEntries { get; }

		protected override int MaxDiscoveryRetries => 3;
		protected override bool DeferFirstDiscovery => true;

		public StoryMessageHandler(KScreen screen) : base(screen) {
			HelpEntries = BuildHelpEntries();
		}

		public override bool DiscoverWidgets(KScreen screen) {
			_widgets.Clear();

			var traverse = Traverse.Create(screen);

			var titleLabel = traverse.Field("titleLabel").GetValue<LocText>();
			var bodyLabel = traverse.Field("bodyLabel").GetValue<LocText>();

			string title = titleLabel != null ? titleLabel.text : null;
			string body = bodyLabel != null ? bodyLabel.text : null;

			string combined = null;
			if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(body))
				combined = title + ". " + body;
			else if (!string.IsNullOrEmpty(title))
				combined = title;
			else if (!string.IsNullOrEmpty(body))
				combined = body;

			if (!string.IsNullOrEmpty(combined)) {
				_widgets.Add(new LabelWidget {
					Label = combined,
					GameObject = screen.gameObject
				});
			}

			var button = traverse.Field("button").GetValue<KButton>();
			if (button != null) {
				_widgets.Add(new ButtonWidget {
					Label = GetButtonLabel(button, (string)STRINGS.UI.CONFIRMDIALOG.OK),
					Component = button,
					GameObject = button.gameObject
				});
			}

			Util.Log.Debug($"StoryMessageHandler.DiscoverWidgets: {_widgets.Count} widgets");
			return true;
		}
	}
}
