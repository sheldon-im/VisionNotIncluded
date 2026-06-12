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

			// Oil reservoirs, Thermal Gas Fissures, and Tidal Springs are
			// IEntityConfig, not IBuildingConfig, so they aren't in
			// Components.Geysers or Components.BuildingCompletes. Each
			// registers a BuildingAttachPoint for its tamer building, which
			// is how we find them.
			foreach (var attachPoint in Components.BuildingAttachPoints.GetWorldItems(worldId)) {
				string subcategory = HardpointSubcategory(attachPoint);
				if (subcategory == null) continue;
				var go = attachPoint.gameObject;
				int cell = Grid.PosToCell(go.transform.GetPosition());
				if (!Grid.IsVisible(cell)) continue;
				yield return MakeEntry(go, cell, subcategory);
			}
		}

		private static string HardpointSubcategory(BuildingAttachPoint attachPoint) {
			var points = attachPoint.points;
			for (int i = 0; i < points.Length; i++) {
				var type = points[i].attachableType;
				if (type == GameTags.OilWell)
					return ScannerTaxonomy.Subcategories.Liquid;
				// Thermal Gas Fissure (emits natural gas, Marine Drill attaches)
				if (type == GameTags.UnderwaterVentDrill)
					return ScannerTaxonomy.Subcategories.Gas;
				// Tidal Spring (breathes liquid, Tidal Turbine attaches)
				if (type == GameTags.ReefGenerator)
					return ScannerTaxonomy.Subcategories.Liquid;
			}
			return null;
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
