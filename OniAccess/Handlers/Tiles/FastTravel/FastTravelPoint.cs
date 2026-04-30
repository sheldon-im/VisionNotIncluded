namespace OniAccess.Handlers.Tiles.FastTravel {
	/// <summary>
	/// One named fast-travel bookmark. Persisted to YAML in the colony folder.
	/// Cell is global across the cluster; WorldId is stored so the menu can
	/// filter to the active world (each asteroid has its own list).
	/// </summary>
	public class FastTravelPoint {
		public string Id { get; set; }
		public string Name { get; set; }
		public int WorldId { get; set; }
		public int Cell { get; set; }
	}
}
