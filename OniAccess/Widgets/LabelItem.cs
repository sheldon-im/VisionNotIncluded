using System;
using System.Collections.Generic;

namespace OniAccess.Widgets {
	/// <summary>
	/// A minimal NavItem backed by a text source, for rows that have no live UI
	/// control to wrap: data rows, synthetic group headers, label-only menu
	/// items, and assembled table cells. The text is read on each
	/// <see cref="Announce"/> so nothing is cached — pass a <see cref="Func{T}"/>
	/// for rows whose value changes between announcements, or a plain string when
	/// the caller already computed it live for this one announcement.
	/// </summary>
	public class LabelItem: NavItem {
		private readonly Func<string> _read;

		public LabelItem(Func<string> read) {
			_read = read;
		}

		public LabelItem(string text) {
			_read = () => text;
		}

		public string RoleKey => null;
		public bool IsNavigable() => true;
		public bool IsActivatable() => false;
		public string Announce() => _read();
		public string SearchText => _read();
		public string ContextLabel => _read();
		public bool Activate() => false;
		public bool Adjust(int direction, int stepLevel) => false;
		public IReadOnlyList<NavItem> GetChildren() => Array.Empty<NavItem>();
	}
}
