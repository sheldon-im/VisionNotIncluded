namespace OniAccess.Widgets {
	/// <summary>
	/// Single home for the verbose "X of Y" counting rule shared by every navigation
	/// surface (flat lists, widget screens, trees, tables). Each surface supplies its
	/// own item count and validity predicate; the rank-and-total computation stays in
	/// one place so a change to the rule can't silently drift between handlers.
	/// </summary>
	public static class NavPosition {
		/// <summary>
		/// 1-based rank of the item at <paramref name="index"/> among the valid items
		/// of a list of <paramref name="count"/>, plus the valid-item total. Position is
		/// -1 (which suppresses the readout) when the index is out of range or invalid.
		/// </summary>
		public static (int position, int total) RankAmongValid(
				int count, System.Func<int, bool> isValid, int index) {
			int total = 0, position = -1;
			for (int i = 0; i < count; i++) {
				if (!isValid(i)) continue;
				total++;
				if (i == index) position = total;
			}
			return (position, total);
		}
	}
}
