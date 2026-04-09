using System.Collections.Generic;
using UnityEngine;

namespace OniAccess.Handlers.Tiles.Scanner.Backends {
	/// <summary>
	/// Backend for the Geysers category. Iterates Components.Geysers and
	/// Components.GeothermalVents, subcategorized by geyser shape.
	/// </summary>
	public class GeyserBackend: IScannerBackend {

		public IEnumerable<ScanEntry> Scan(int worldId) {
			foreach (var geyser in Components.Geysers.GetItems(worldId)) {
				var go = geyser.gameObject;
				int cell = Grid.PosToCell(go.transform.GetPosition());
				if (!Grid.IsVisible(cell)) continue;
				var uncoverable = go.GetComponent<Uncoverable>();
				if (uncoverable != null && !uncoverable.IsUncovered) continue;
				yield return MakeEntry(go, cell, ShapeSubcategory(geyser));
			}

			foreach (var vent in Components.GeothermalVents.GetItems(worldId)) {
				var go = vent.gameObject;
				int cell = Grid.PosToCell(go.transform.GetPosition());
				if (!Grid.IsVisible(cell)) continue;
				var uncoverable = go.GetComponent<Uncoverable>();
				if (uncoverable != null && !uncoverable.IsUncovered) continue;
				yield return MakeEntry(go, cell, ScannerTaxonomy.Subcategories.Geothermal);
			}

			// Oil reservoirs are IEntityConfig, not IBuildingConfig, so they
			// aren't in Components.Geysers or Components.BuildingCompletes.
			// They do register a BuildingAttachPoint with GameTags.OilWell,
			// which is how we find them.
			foreach (var attachPoint in Components.BuildingAttachPoints.GetWorldItems(worldId)) {
				if (!HasOilWellHardpoint(attachPoint)) continue;
				var go = attachPoint.gameObject;
				int cell = Grid.PosToCell(go.transform.GetPosition());
				if (!Grid.IsVisible(cell)) continue;
				yield return MakeEntry(go, cell, ScannerTaxonomy.Subcategories.Liquid);
			}
		}

		private static bool HasOilWellHardpoint(BuildingAttachPoint attachPoint) {
			var points = attachPoint.points;
			for (int i = 0; i < points.Length; i++)
				if (points[i].attachableType == GameTags.OilWell)
					return true;
			return false;
		}

		public bool ValidateEntry(ScanEntry entry, int cursorCell) {
			var go = (GameObject)entry.BackendData;
			if (go == null || go.IsNullOrDestroyed()) return false;
			var uncoverable = go.GetComponent<Uncoverable>();
			if (uncoverable != null && !uncoverable.IsUncovered) return false;
			return Grid.IsVisible(entry.Cell);
		}

		public string FormatName(ScanEntry entry) {
			var go = (GameObject)entry.BackendData;
			return GetGeyserName(go) ?? entry.ItemName;
		}

		private ScanEntry MakeEntry(GameObject go, int cell, string subcategory) {
			string name = GetGeyserName(go) ?? go.name;
			return new ScanEntry {
				Cell = cell,
				Backend = this,
				BackendData = go,
				Category = ScannerTaxonomy.Categories.Geysers,
				Subcategory = subcategory,
				ItemName = name,
			};
		}

		private static string ShapeSubcategory(Geyser geyser) {
			switch (geyser.configuration.geyserType.shape) {
				case GeyserConfigurator.GeyserShape.Gas: return ScannerTaxonomy.Subcategories.Gas;
				case GeyserConfigurator.GeyserShape.Liquid: return ScannerTaxonomy.Subcategories.Liquid;
				case GeyserConfigurator.GeyserShape.Molten: return ScannerTaxonomy.Subcategories.Molten;
				default: return ScannerTaxonomy.Subcategories.Gas;
			}
		}

		private static string GetGeyserName(GameObject go) {
			var userNameable = go.GetComponent<UserNameable>();
			if (userNameable != null && !string.IsNullOrEmpty(userNameable.savedName))
				return userNameable.savedName;
			return go.GetComponent<KSelectable>()?.GetName();
		}
	}
}
