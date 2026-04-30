using System;
using System.Collections.Generic;

using HarmonyLib;
using OniAccess.Input;
using OniAccess.Speech;
using OniAccess.Util;
using UnityEngine;

namespace OniAccess.Handlers {
	/// <summary>
	/// Modal prompt that asks the user for a single line of text. Owns a
	/// synthetic KInputTextField (cloned from the FileNameDialog prefab and
	/// parented offscreen under globalCanvas) so the editing experience is
	/// driven by the same TextEditHelper used for in-game text fields.
	///
	/// On confirm: invokes onConfirm with the entered text and pops the handler.
	/// On cancel (Escape): pops the handler without invoking onConfirm.
	/// </summary>
	public class TextPromptHandler: IAccessHandler {
		private readonly string _displayName;
		private readonly string _initialText;
		private readonly Action<string> _onConfirm;
		private readonly TextEditHelper _textEdit = new TextEditHelper();
		private GameObject _instanceGo;
		private KInputTextField _field;

		private static readonly IReadOnlyList<HelpEntry> _helpEntries = new List<HelpEntry> {
			new HelpEntry("Enter", STRINGS.ONIACCESS.HELP.SELECT_ITEM),
			new HelpEntry("Escape", STRINGS.ONIACCESS.HELP.CLOSE),
		}.AsReadOnly();

		public TextPromptHandler(string displayName, string initialText, Action<string> onConfirm) {
			_displayName = displayName;
			_initialText = initialText ?? "";
			_onConfirm = onConfirm;
		}

		public string DisplayName => _displayName;
		public bool CapturesAllInput => true;
		public IReadOnlyList<HelpEntry> HelpEntries => _helpEntries;
		public IReadOnlyList<ConsumedKey> ConsumedKeys { get; } = Array.Empty<ConsumedKey>();

		public void OnActivate() {
			SpeechPipeline.SpeakInterrupt(_displayName);
			if (!CreateSyntheticField()) {
				Log.Warn("TextPromptHandler: failed to create synthetic input field");
				HandlerStack.Pop();
				return;
			}
			_textEdit.Begin(() => _field, _initialText, onEnd: OnEditEnd);
		}

		public void OnDeactivate() {
			DestroySyntheticField();
		}

		public bool Tick() {
			return _textEdit.HandleTick();
		}

		public bool HandleKeyDown(KButtonEvent e) {
			return _textEdit.HandleKeyDown(e);
		}

		private void OnEditEnd() {
			bool wasCancelled = _textEdit.WasCancelled;
			string text = _field.text ?? "";
			// Fire the callback BEFORE popping so the parent's OnActivate (which
			// HandlerStack.Pop triggers) sees any state the callback mutates.
			if (!wasCancelled) {
				try {
					_onConfirm.Invoke(text);
				} catch (System.Exception ex) {
					Log.Error($"TextPromptHandler.onConfirm failed: {ex}");
				}
			}
			HandlerStack.Pop();
		}

		private bool CreateSyntheticField() {
			if (ScreenPrefabs.Instance == null) return false;
			var prefabDialog = ScreenPrefabs.Instance.FileNameDialog;
			if (prefabDialog == null) return false;
			var prefabField = Traverse.Create(prefabDialog).Field("inputField").GetValue<KInputTextField>();
			if (prefabField == null || prefabField.gameObject == null) return false;

			var canvas = Global.Instance != null ? Global.Instance.globalCanvas : null;
			if (canvas == null) return false;

			try {
				_instanceGo = UnityEngine.Object.Instantiate(
					prefabField.gameObject, canvas.transform, worldPositionStays: false);
			} catch (Exception ex) {
				Log.Warn($"TextPromptHandler: Instantiate failed: {ex.Message}");
				return false;
			}

			_field = _instanceGo.GetComponent<KInputTextField>();
			if (_field == null) {
				UnityEngine.Object.Destroy(_instanceGo);
				_instanceGo = null;
				return false;
			}

			// Hide visually — the mod is speech-only, no need to render the field.
			var rt = _instanceGo.GetComponent<RectTransform>();
			if (rt != null) rt.anchoredPosition = new Vector2(-100000, -100000);

			return true;
		}

		private void DestroySyntheticField() {
			if (_instanceGo != null) {
				UnityEngine.Object.Destroy(_instanceGo);
				_instanceGo = null;
				_field = null;
			}
		}
	}
}
