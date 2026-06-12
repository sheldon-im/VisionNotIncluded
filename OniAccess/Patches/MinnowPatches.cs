using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using OniAccess.Handlers.Tiles;
using OniAccess.Handlers.Tiles.Scanner;
using OniAccess.Speech;
using OniAccess.Util;
using UnityEngine;

namespace OniAccess.Patches {
	/// <summary>
	/// Announcements for the Minnow quest completion sequence (Aquatic DLC).
	/// Acknowledging a completion popup drops reward items at the site and,
	/// for non-final quests, reveals the next site and pans the camera to it
	/// during an input-locked cinematic. Both beats are visual-only.
	/// </summary>
	[HarmonyPatch(typeof(MinnowImperativePOIStates), "SpawnReward")]
	internal static class MinnowImperativePOIStates_SpawnReward_Patch {
		static void Prefix(MinnowImperativePOIStates.Instance smi, out HashSet<int> __state) {
			__state = null;
			if (!ModToggle.IsEnabled) return;
			try {
				var before = new HashSet<int>();
				foreach (int cell in RewardCells(smi))
					CollectPickupables(cell, go => before.Add(go.GetInstanceID()));
				__state = before;
			} catch (System.Exception ex) {
				Log.Error($"MinnowImperativePOIStates_SpawnReward_Patch.Prefix: {ex}");
			}
		}

		static void Postfix(MinnowImperativePOIStates.Instance smi, HashSet<int> __state) {
			if (!ModToggle.IsEnabled || __state == null) return;
			try {
				Game.Instance.StartCoroutine(AnnounceAfterSpawn(smi, __state));
			} catch (System.Exception ex) {
				Log.Error($"MinnowImperativePOIStates_SpawnReward_Patch.Postfix: {ex}");
			}
		}

		/// <summary>
		/// Spawned pickupables register on the grid in OnSpawn, a frame after
		/// SpawnReward instantiates them, so the diff must wait a frame.
		/// </summary>
		private static IEnumerator AnnounceAfterSpawn(
			MinnowImperativePOIStates.Instance smi, HashSet<int> before) {
			yield return null;
			AnnounceRewards(smi, before);
		}

		private static void AnnounceRewards(
			MinnowImperativePOIStates.Instance smi, HashSet<int> before) {
			try {
				var counts = new Dictionary<string, int>();
				foreach (int cell in RewardCells(smi))
					CollectPickupables(cell, go => {
						if (before.Contains(go.GetInstanceID())) return;
						string name = DebrisNameHelper.GetDisplayName(go);
						counts.TryGetValue(name, out int n);
						counts[name] = n + 1;
					});
				if (counts.Count == 0) {
					Log.Warn("MinnowPatches: no reward pickupables found after SpawnReward");
					return;
				}

				var parts = new List<string>();
				foreach (var kvp in counts)
					parts.Add(kvp.Value > 1
						? string.Format((string)STRINGS.ONIACCESS.MINNOW.REWARD_COUNT,
							kvp.Value, kvp.Key)
						: kvp.Key);
				SpeechPipeline.SpeakInterrupt(string.Format(
					(string)STRINGS.ONIACCESS.MINNOW.REWARDS, string.Join(", ", parts)));
			} catch (System.Exception ex) {
				Log.Error($"MinnowImperativePOIStates_SpawnReward_Patch.AnnounceRewards: {ex}");
			}
		}

		/// <summary>
		/// SpawnReward drops items at the POI's cell and one cell to each side.
		/// </summary>
		private static int[] RewardCells(MinnowImperativePOIStates.Instance smi) {
			int cell = Grid.PosToCell(smi.gameObject);
			return new[] { cell, Grid.CellLeft(cell), Grid.CellRight(cell) };
		}

		private static void CollectPickupables(int cell, System.Action<GameObject> visit) {
			if (!Grid.IsValidCell(cell)) return;
			var go = Grid.Objects[cell, (int)ObjectLayer.Pickupables];
			if (go == null) return;
			var pickupable = go.GetComponent<Pickupable>();
			if (pickupable == null) return;
			var listItem = pickupable.objectLayerListItem;
			while (listItem != null) {
				var itemGo = listItem.gameObject;
				listItem = listItem.nextItem;
				if (itemGo != null) visit(itemGo);
			}
		}
	}

	[HarmonyPatch(typeof(MinnowImperativePOIStates.Instance), "FindNextUncompletedPOIPosition")]
	internal static class MinnowImperativePOIStates_FindNextUncompletedPOIPosition_Patch {
		static void Postfix(bool __result, ref Vector3 position) {
			if (!ModToggle.IsEnabled || !__result) return;
			try {
				string distance = AnnouncementFormatter.FormatDistance(
					TileCursor.Instance.Cell, Grid.PosToCell(position));
				// All three sites share one name, so site A's string covers
				// the not-yet-spawned next site too
				string name = STRINGS.BUILDINGS.PREFABS.MINNOW_IMPERATIVE_POI_A.NAME;
				SpeechPipeline.SpeakQueued(string.Format(
					(string)STRINGS.ONIACCESS.MINNOW.NEXT_SITE, name, distance));
			} catch (System.Exception ex) {
				Log.Error($"MinnowImperativePOIStates_FindNextUncompletedPOIPosition_Patch: {ex}");
			}
		}
	}
}
