using System;
using System.Collections.Generic;

namespace OniAccess.Widgets {
	/// <summary>
	/// A general-purpose <see cref="NavItem"/> for menu trees built from game data
	/// rather than live UI controls. Its label, children, and activation are supplied
	/// as delegates, so a handler can describe a whole drill tree declaratively without
	/// a class per node type.
	///
	/// Like <see cref="LabelItem"/>, nothing is stored: the label is read on each
	/// <see cref="Announce"/> and children are computed on each <see cref="GetChildren"/>,
	/// so the tree always reflects live, filtered, or dynamic content. Children are the
	/// lazy <c>children()</c> the unified engine walks.
	/// </summary>
	public sealed class MenuNode: NavItem {
		private readonly Func<string> _announce;
		private readonly Func<string> _searchText;
		private readonly Func<string> _contextLabel;
		private readonly Func<string> _tooltip;
		private readonly Func<IReadOnlyList<NavItem>> _children;
		private readonly Func<bool> _activate;
		private readonly bool _navigable;

		public MenuNode(
				Func<string> announce,
				Func<IReadOnlyList<NavItem>> children = null,
				Func<bool> activate = null,
				bool navigable = true,
				string roleKey = null,
				Func<string> searchText = null,
				Func<string> contextLabel = null,
				Func<string> tooltip = null) {
			_announce = announce;
			_searchText = searchText;
			_contextLabel = contextLabel;
			_tooltip = tooltip;
			_children = children;
			_activate = activate;
			_navigable = navigable;
			RoleKey = roleKey;
		}

		public string RoleKey { get; }
		public bool IsNavigable() => _navigable;
		public bool IsActivatable() => _activate != null;
		public string Announce() => _announce();

		/// <summary>Search text, when it differs from the spoken label; otherwise the label.</summary>
		public string SearchText => _searchText != null ? _searchText() : _announce();

		/// <summary>Parent-context label, when it differs from the spoken label; otherwise the label.</summary>
		public string ContextLabel => _contextLabel != null ? _contextLabel() : _announce();

		/// <summary>Supplementary text appended through the tooltip slot, or null when none.</summary>
		public string Tooltip => _tooltip?.Invoke();
		public bool Activate() => _activate != null && _activate();
		public bool Adjust(int direction, int stepLevel) => false;

		public IReadOnlyList<NavItem> GetChildren() =>
			_children?.Invoke() ?? Array.Empty<NavItem>();
	}
}
