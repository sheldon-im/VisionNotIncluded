using System.Collections.Generic;

namespace OniAccess.Handlers.Tiles.Scanner {
	/// <summary>
	/// Backend interface for scanner data sources. Backends are stateless
	/// between refreshes — they query game state fresh in Scan() and
	/// return ScanEntry objects.
	/// </summary>
	public interface IScannerBackend {
		/// <summary>
		/// Query game state and return entries for the given world.
		/// Cursor position is not passed here — distance sorting happens
		/// in ScannerSnapshot after all backends return.
		/// </summary>
		IEnumerable<ScanEntry> Scan(int worldId);

		/// <summary>
		/// Validate that an entry is still current. Called when the user
		/// navigates to an instance. Returns false if the entry is stale
		/// and should be removed from the snapshot.
		/// </summary>
		bool ValidateEntry(ScanEntry entry, int cursorCell);

		/// <summary>
		/// Return the spoken label for this instance.
		/// </summary>
		string FormatName(ScanEntry entry);
	}

	/// <summary>
	/// Capability for backends that consume GridScanner output. The navigator
	/// hands the whole scan result to each consumer before calling Scan(); the
	/// backend pulls the fields it needs. Keeps grid-to-backend wiring out of
	/// the navigator so adding a clustered source is a one-line list edit.
	/// </summary>
	public interface IGridConsumerBackend {
		void SetGridData(GridScanResult grid);
	}

	/// <summary>
	/// Capability for backends that speak a supplemental detail after the item
	/// name (e.g. element mass). Optional: the navigator queries it only when
	/// the backend implements it, so the navigator stays blind to backend types.
	/// </summary>
	public interface IDetailBackend {
		/// <summary>Detail clause to append after the name, or null for none.</summary>
		string FormatDetail(ScanEntry entry);
	}
}
