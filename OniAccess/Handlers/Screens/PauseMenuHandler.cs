using System.Collections.Generic;
using HarmonyLib;

using OniAccess.Widgets;
namespace OniAccess.Handlers.Screens {
	/// <summary>
	/// Handler for the pause menu (PauseScreen class).
	/// PauseScreen inherits KModalButtonMenu (which inherits KButtonMenu), so we
	/// use the buttons array pattern: KButtonMenu.buttons provides ButtonInfo labels,
	/// KButtonMenu.buttonObjects provides the GameObjects with KButton components.
	///
	/// Per Pitfall 3: RefreshButtons() destroys cached references. The base class
	/// already calls DiscoverWidgets on OnActivate, so references are always fresh.
	/// </summary>
	public class PauseMenuHandler: BaseWidgetHandler {
		public override string DisplayName => STRINGS.ONIACCESS.HANDLERS.PAUSE_MENU;

		public override IReadOnlyList<HelpEntry> HelpEntries { get; }

		public PauseMenuHandler(KScreen screen) : base(screen) {
			HelpEntries = BuildHelpEntries();
		}

		public override void OnActivate() {
			base.OnActivate();
			try {
				string coords = CustomGameSettings.Instance.GetSettingsCoordinate();
				if (!string.IsNullOrEmpty(coords)) {
					Speech.SpeechPipeline.SpeakQueued(
						string.Format(STRINGS.UI.FRONTEND.PAUSE_SCREEN.WORLD_SEED, coords));
				}
			} catch (System.Exception ex) {
				Util.Log.Warn($"PauseMenuHandler: failed to read world seed: {ex}");
			}
		}

		public override bool DiscoverWidgets(KScreen screen) {
			_widgets.Clear();

			// KButtonMenu.buttons is an IList of ButtonInfo structs with .text labels
			var buttons = Traverse.Create(screen).Field("buttons")
				.GetValue<System.Collections.IList>();
			// KButtonMenu.buttonObjects is the array of instantiated GameObjects
			var buttonObjects = Traverse.Create(screen).Field("buttonObjects")
				.GetValue<UnityEngine.GameObject[]>();

			if (buttons == null || buttonObjects == null) return true;

			int count = System.Math.Min(buttons.Count, buttonObjects.Length);
			for (int i = 0; i < count; i++) {
				if (buttonObjects[i] == null || !buttonObjects[i].activeInHierarchy) continue;

				var kbutton = buttonObjects[i].GetComponent<KButton>();
				if (kbutton == null || !kbutton.isInteractable) continue;

				string label = Traverse.Create(buttons[i]).Field("text")
					.GetValue<string>();
				if (string.IsNullOrEmpty(label)) continue;

				_widgets.Add(new ButtonWidget {
					Label = label,
					Component = kbutton,
					GameObject = buttonObjects[i]
				});
			}

			AddWorldSeedCopyButton(screen);

			Util.Log.Debug($"PauseMenuHandler.DiscoverWidgets: {_widgets.Count} widgets");
			return true;
		}

		private void AddWorldSeedCopyButton(KScreen screen) {
			try {
				var clipboard = Traverse.Create(screen).Field("clipboard")
					.GetValue<CopyTextFieldToClipboard>();
				if (clipboard == null) return;

				var kbutton = Traverse.Create(clipboard).Field("button")
					.GetValue<KButton>();
				if (kbutton == null || !kbutton.isInteractable) return;

				var buttonGO = kbutton.gameObject;
				if (buttonGO == null || !buttonGO.activeInHierarchy) return;

				string label = (string)STRINGS.UI.CRASHSCREEN.COPYTOCLIPBOARDBUTTON;
				if (string.IsNullOrEmpty(label))
					label = (string)STRINGS.UI.FRONTEND.PAUSE_SCREEN.WORLD_SEED_COPY_TOOLTIP;
				if (string.IsNullOrEmpty(label)) return;

				var widget = new ButtonWidget {
					Label = label,
					Component = kbutton,
					GameObject = buttonGO
				};

				// The copy control is visually grouped with the world seed at the top of the
				// pause screen.  Insert it after the first normal pause action so it is easy
				// to find without changing the initial "continue" focus.
				if (_widgets.Count > 0)
					_widgets.Insert(1, widget);
				else
					_widgets.Add(widget);
			} catch (System.Exception ex) {
				Util.Log.Warn($"PauseMenuHandler: failed to add world seed copy button: {ex}");
			}
		}
	}
}
