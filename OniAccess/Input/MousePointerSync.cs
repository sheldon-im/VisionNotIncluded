using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using OniAccess.Util;
using OniAccess.Widgets;

namespace OniAccess.Input {
	/// <summary>
	/// Owns the KInputManager mouse lock while UI widget handlers are active,
	/// so a sighted observer can see the OS pointer and hover state track the
	/// blind user's logical cursor. Also best-effort scrolls ancestor ScrollRects
	/// to bring off-screen widgets into view before positioning the pointer.
	///
	/// Push/Pop bracket each BaseWidgetHandler's lifetime so the pre-handler
	/// lock state (usually the tile cursor's cell lock) is restored on pop.
	/// Nested widget handlers stack cleanly: each saves its predecessor's state.
	/// </summary>
	public static class MousePointerSync {
		private struct LockState {
			public bool WasLocked;
			public Vector3 Position;
		}

		private static readonly Stack<LockState> _stack = new Stack<LockState>();

		/// <summary>
		/// Save the current mouse-lock state. Call from widget handler OnActivate.
		/// </summary>
		public static void Push() {
			_stack.Push(new LockState {
				WasLocked = KInputManager.isMousePosLocked,
				Position = KInputManager.lockedMousePos,
			});
		}

		/// <summary>
		/// Restore the mouse-lock state saved by the matching Push.
		/// Call from widget handler OnDeactivate.
		/// </summary>
		public static void Pop() {
			if (_stack.Count == 0) {
				Log.Warn("MousePointerSync.Pop called on empty stack");
				return;
			}
			var s = _stack.Pop();
			KInputManager.isMousePosLocked = s.WasLocked;
			KInputManager.lockedMousePos = s.Position;
		}

		/// <summary>
		/// Move the OS pointer to the given screen-space position and keep it
		/// locked there until Pop or a subsequent Sync.
		/// </summary>
		public static void SyncToScreenPos(Vector3 screenPos) {
			KInputManager.isMousePosLocked = true;
			KInputManager.lockedMousePos = screenPos;
		}

		/// <summary>
		/// Scroll ancestor ScrollRects to bring the widget into view, then move
		/// the OS pointer to the widget's center. No-op if widget is null,
		/// inactive, or has no RectTransform.
		/// </summary>
		public static void SyncToWidget(Widget widget) {
			if (widget == null || widget.GameObject == null) return;
			if (!widget.GameObject.activeInHierarchy) return;

			var rect = widget.GameObject.GetComponent<RectTransform>();
			if (rect == null) rect = widget.GameObject.GetComponentInParent<RectTransform>();
			if (rect == null) return;

			try {
				ScrollIntoView(rect);
			} catch (System.Exception ex) {
				Log.Warn($"MousePointerSync.ScrollIntoView failed: {ex.Message}");
			}

			var canvas = rect.GetComponentInParent<Canvas>();
			Camera cam = null;
			if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera) {
				cam = canvas.worldCamera;
			}

			Vector3 worldCenter = rect.TransformPoint(rect.rect.center);
			Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldCenter);
			SyncToScreenPos(screenPoint);
		}

		/// <summary>
		/// Best-effort: if the widget is outside its nearest ScrollRect viewport,
		/// offset the content so the widget center falls inside. Only the
		/// immediate ancestor ScrollRect is adjusted.
		/// </summary>
		private static void ScrollIntoView(RectTransform widgetRect) {
			var scrollRect = widgetRect.GetComponentInParent<ScrollRect>();
			if (scrollRect == null || scrollRect.content == null) return;
			if (!scrollRect.IsActive()) return;

			RectTransform viewport = scrollRect.viewport != null
				? scrollRect.viewport
				: scrollRect.transform as RectTransform;
			if (viewport == null) return;

			Vector3 widgetViewportLocal = viewport.InverseTransformPoint(
				widgetRect.TransformPoint(widgetRect.rect.center));
			Rect vp = viewport.rect;

			Vector2 delta = Vector2.zero;
			bool needsScroll = false;

			if (scrollRect.vertical) {
				if (widgetViewportLocal.y > vp.yMax) {
					delta.y = widgetViewportLocal.y - vp.yMax;
					needsScroll = true;
				} else if (widgetViewportLocal.y < vp.yMin) {
					delta.y = widgetViewportLocal.y - vp.yMin;
					needsScroll = true;
				}
			}
			if (scrollRect.horizontal) {
				if (widgetViewportLocal.x > vp.xMax) {
					delta.x = widgetViewportLocal.x - vp.xMax;
					needsScroll = true;
				} else if (widgetViewportLocal.x < vp.xMin) {
					delta.x = widgetViewportLocal.x - vp.xMin;
					needsScroll = true;
				}
			}

			if (!needsScroll) return;

			RectTransform content = scrollRect.content;
			content.anchoredPosition = new Vector2(
				content.anchoredPosition.x - delta.x,
				content.anchoredPosition.y - delta.y);
			scrollRect.normalizedPosition = new Vector2(
				Mathf.Clamp01(scrollRect.normalizedPosition.x),
				Mathf.Clamp01(scrollRect.normalizedPosition.y));
		}
	}
}
