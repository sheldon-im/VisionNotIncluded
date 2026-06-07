namespace OniAccess.Handlers.Tiles.Scanner {
	/// <summary>
	/// One instance in the 4-level scanner hierarchy.
	/// Multiple ScanEntries with the same Category/Subcategory/ItemName
	/// form the instance list for that item.
	/// </summary>
	public class ScanEntry {
		public int Cell;
		public IScannerBackend Backend;
		// Owned and interpreted solely by Backend. The navigator only ever
		// dispatches an entry to its own Backend (ValidateEntry/FormatName), so
		// each backend's cast back to its concrete type is total, not a guess.
		public object BackendData;
		public string Category;
		public string Subcategory;
		public string ItemName;
		public int SortKey;
	}
}
