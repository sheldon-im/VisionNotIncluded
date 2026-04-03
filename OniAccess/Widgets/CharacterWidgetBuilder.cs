using System.Collections.Generic;
using HarmonyLib;
using Klei.AI;

namespace OniAccess.Widgets {
	/// <summary>
	/// Shared widget-building logic for CharacterContainer screens
	/// (MinionSelectScreen, ImmigrantScreen). Each method appends widgets
	/// to the caller's list, wrapped in try/catch with the caller's name
	/// for log context.
	/// </summary>
	public static class CharacterWidgetBuilder {
		/// <summary>
		/// Appends the duplicant name label and bionic model label (if applicable).
		/// Does not add rename/shuffle buttons — callers that need those add them separately.
		/// </summary>
		public static void AddNameWidgets(List<Widget> widgets, Traverse traverse, string caller) {
			try {
				var titleBar = traverse.Field("characterNameTitle").GetValue<object>();
				if (titleBar == null) return;

				var locText = Traverse.Create(titleBar).Field("titleText").GetValue<LocText>();
				if (locText == null || string.IsNullOrEmpty(locText.text)) return;

				widgets.Add(new LabelWidget {
					Label = locText.text,
					GameObject = locText.gameObject
				});

				var stats = traverse.Field("stats").GetValue<MinionStartingStats>();
				if (stats != null && stats.personality.model == GameTags.Minions.Models.Bionic) {
					widgets.Add(new LabelWidget {
						Label = (string)STRINGS.DUPLICANTS.MODEL.BIONIC.NAME,
						GameObject = locText.gameObject,
						Tag = "model_type"
					});
				}
			} catch (System.Exception ex) {
				Util.Log.Error($"{caller}.AddNameWidgets: {ex.Message}");
			}
		}

		/// <summary>
		/// Appends one LabelWidget per interest (aptitude entry).
		/// </summary>
		public static void AddInterestWidgets(List<Widget> widgets, Traverse traverse, string caller) {
			try {
				var aptitudeEntries = traverse.Field("aptitudeEntries")
					.GetValue<List<UnityEngine.GameObject>>();
				if (aptitudeEntries == null) return;

				foreach (var entryGo in aptitudeEntries) {
					if (entryGo == null || !entryGo.activeInHierarchy) continue;
					var locTexts = entryGo.GetComponentsInChildren<LocText>(false);
					if (locTexts == null || locTexts.Length == 0) continue;

					var parts = new List<string>();
					foreach (var lt in locTexts) {
						if (lt == null || string.IsNullOrEmpty(lt.text)
							|| !lt.gameObject.activeInHierarchy) continue;
						parts.Add(lt.text.Trim());
					}

					if (parts.Count > 0) {
						widgets.Add(new LabelWidget {
							Label = $"{STRINGS.ONIACCESS.INFO.INTEREST}: {string.Join(", ", parts)}",
							GameObject = entryGo,
							Tag = "interest"
						});
					}
				}
			} catch (System.Exception ex) {
				Util.Log.Error($"{caller}.AddInterestWidgets: {ex.Message}");
			}
		}

		/// <summary>
		/// Appends one LabelWidget per trait with positive/negative or bionic upgrade/bug prefix
		/// and flattened tooltip text.
		/// </summary>
		public static void AddTraitWidgets(List<Widget> widgets, MinionStartingStats stats, UnityEngine.GameObject containerGo, string caller) {
			try {
				var traits = stats.Traits;
				if (traits == null) return;

				bool isBionic = stats.personality.model == GameTags.Minions.Models.Bionic;

				// Skip index 0 (same as game's SetInfoText does)
				for (int i = 1; i < traits.Count; i++) {
					var trait = traits[i];
					string name = trait.GetName();
					if (string.IsNullOrEmpty(name)) continue;

					string prefix;
					if (isBionic) {
						prefix = trait.PositiveTrait
							? (string)STRINGS.ONIACCESS.INFO.BIONIC_UPGRADE
							: (string)STRINGS.ONIACCESS.INFO.BIONIC_BUG;
					} else {
						prefix = trait.PositiveTrait
							? (string)STRINGS.ONIACCESS.INFO.POSITIVE_TRAIT
							: (string)STRINGS.ONIACCESS.INFO.NEGATIVE_TRAIT;
					}

					string tooltip = trait.GetTooltip();
					string label;
					if (string.IsNullOrEmpty(tooltip)) {
						label = $"{prefix}: {name}";
					} else {
						string flat = tooltip.Replace("\n• ", ", ").Replace("\n", ", ");
						label = $"{prefix}: {name}, {flat}";
					}

					widgets.Add(new LabelWidget {
						Label = label,
						GameObject = containerGo
					});
				}
			} catch (System.Exception ex) {
				Util.Log.Error($"{caller}.AddTraitWidgets: {ex.Message}");
			}
		}

		/// <summary>
		/// Appends one LabelWidget per expectation with tooltip text.
		/// </summary>
		public static void AddExpectationWidgets(List<Widget> widgets, Traverse traverse, string caller) {
			try {
				var labels = traverse.Field("expectationLabels")
					.GetValue<List<LocText>>();
				if (labels == null) return;

				foreach (var lt in labels) {
					if (lt == null || string.IsNullOrEmpty(lt.text)
						|| !lt.gameObject.activeInHierarchy) continue;

					string label = lt.text.Trim();
					var tooltip = lt.GetComponent<ToolTip>();
					if (tooltip != null) {
						try {
							string ttText = WidgetOps.ReadAllTooltipText(tooltip);
							if (!string.IsNullOrEmpty(ttText)) {
								label = $"{label}, {ttText}";
							}
						} catch (System.Exception ex) {
							Util.Log.Error($"{caller}.AddExpectationWidgets(tooltip): {ex.Message}");
						}
					}

					widgets.Add(new LabelWidget {
						Label = label,
						GameObject = lt.gameObject
					});
				}
			} catch (System.Exception ex) {
				Util.Log.Error($"{caller}.AddExpectationWidgets: {ex.Message}");
			}
		}

		/// <summary>
		/// Appends one LabelWidget per attribute with tooltip text.
		/// </summary>
		public static void AddAttributeWidgets(List<Widget> widgets, Traverse traverse, string caller) {
			try {
				var iconGroups = traverse.Field("iconGroups")
					.GetValue<List<UnityEngine.GameObject>>();
				if (iconGroups == null) return;

				foreach (var go in iconGroups) {
					if (go == null || !go.activeInHierarchy) continue;
					var locText = go.GetComponentInChildren<LocText>();
					if (locText == null || string.IsNullOrEmpty(locText.text)) continue;

					string label = locText.text.Trim();

					var tooltip = go.GetComponent<ToolTip>();
					if (tooltip != null) {
						try {
							string ttText = WidgetOps.ReadAllTooltipText(tooltip);
							if (!string.IsNullOrEmpty(ttText)) {
								string flat = ttText.Replace("\n", ", ").Replace("\r", "");
								label = $"{label}, {flat}";
							}
						} catch (System.Exception ex) {
							Util.Log.Error($"{caller}.AddAttributeWidgets(tooltip): {ex.Message}");
						}
					}

					widgets.Add(new LabelWidget {
						Label = label,
						GameObject = go
					});
				}
			} catch (System.Exception ex) {
				Util.Log.Error($"{caller}.AddAttributeWidgets: {ex.Message}");
			}
		}

		/// <summary>
		/// Appends description widgets: mod-authored personality description (if available)
		/// and the game's bio text.
		/// </summary>
		public static void AddDescriptionWidgets(List<Widget> widgets, Traverse traverse, MinionStartingStats stats, UnityEngine.GameObject containerGo, string caller) {
			try {
				string key = "STRINGS.ONIACCESS.DUPE_DESCRIPTIONS."
					+ stats.personality.Name.Replace("-", "_").ToUpper();
				if (Strings.TryGet(key, out var entry)) {
					widgets.Add(new LabelWidget {
						Label = string.Format((string)STRINGS.ONIACCESS.INFO.DUPE_DESCRIPTION, entry.String),
						GameObject = containerGo
					});
				}

				var descLocText = traverse.Field("description").GetValue<LocText>();
				if (descLocText != null && !string.IsNullOrEmpty(descLocText.text)) {
					widgets.Add(new LabelWidget {
						Label = descLocText.text.Trim(),
						GameObject = descLocText.gameObject
					});
				}
			} catch (System.Exception ex) {
				Util.Log.Error($"{caller}.AddDescriptionWidgets: {ex.Message}");
			}
		}
	}
}
