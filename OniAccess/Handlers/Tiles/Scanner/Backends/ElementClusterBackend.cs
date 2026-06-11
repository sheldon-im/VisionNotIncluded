using System.Collections.Generic;

namespace OniAccess.Handlers.Tiles.Scanner.Backends {
	/// <summary>
	/// Backend for natural elements (Solids, Liquids, Gases).
	/// Receives pre-clustered data from GridScanner.
	/// </summary>
	public class ElementClusterBackend: IScannerBackend, IGridConsumerBackend, IDetailBackend {
		private List<ElementCluster> _clusters;

		public void SetGridData(GridScanResult grid) {
			_clusters = grid.Elements;
		}

		public IEnumerable<ScanEntry> Scan(int worldId) {
			if (_clusters == null) yield break;
			foreach (var cluster in _clusters) {
				yield return new ScanEntry {
					Cell = cluster.Cells[0],
					Backend = this,
					BackendData = cluster,
					Category = cluster.Category,
					Subcategory = cluster.Subcategory,
					ItemName = cluster.ElementName,
				};
			}
		}

		public bool ValidateEntry(ScanEntry entry, int cursorCell) {
			var cluster = (ElementCluster)entry.BackendData;
			if (cluster.IsBackwall)
				return GridUtil.ValidateCluster(cluster.Cells, cursorCell, entry,
					cell => Grid.IsValidCell(cell) && !Grid.Solid[cell]
						&& BackwallManager.HasBackwall(cell)
						&& BackwallManager.At(cell).Element.id == cluster.ElementId);
			return GridUtil.ValidateCluster(cluster.Cells, cursorCell, entry,
				cell => Grid.IsValidCell(cell)
					&& Grid.Element[cell].id == cluster.ElementId);
		}

		public string FormatName(ScanEntry entry) {
			var cluster = (ElementCluster)entry.BackendData;
			if (cluster.Cells.Count == 1) return cluster.ElementName;
			return string.Format(
				(string)STRINGS.ONIACCESS.SCANNER.CLUSTER_LABEL,
				cluster.Cells.Count, cluster.ElementName);
		}

		// Mass is element-specific, so it lives here rather than in the navigator.
		// Gas clusters speak the per-cell average; everything else the total.
		public string FormatDetail(ScanEntry entry) {
			if (!ConfigManager.Config.ScannerMassReadout) return null;
			var cluster = (ElementCluster)entry.BackendData;
			if (cluster.TotalMass <= 0f) return null;
			if (cluster.Category == ScannerTaxonomy.Categories.Gases
				&& cluster.Cells.Count > 1) {
				string formatted = Sections.ElementSection.FormatGlanceMass(
					cluster.TotalMass / cluster.Cells.Count);
				return string.Format(
					(string)STRINGS.ONIACCESS.SCANNER.MASS_AVERAGE, formatted);
			}
			return Sections.ElementSection.FormatGlanceMass(cluster.TotalMass);
		}

	}
}
