using System.Collections.Generic;

using OniAccess.Speech;

namespace OniAccess.Tests {
	/// <summary>
	/// Offline tests for the Alt+Up/Down line reviewer. The reviewer is a pull:
	/// the caller hands it the focused item's live announcement each press, and it
	/// splits and walks the lines with end clamping, re-splitting when the content
	/// changes. Tests cover the splitter and the stepping/clamping logic.
	/// </summary>
	static class LineReviewTests {
		private static (string, bool, string) Check(string name, bool ok, string detail)
			=> (name, ok, ok ? "OK" : detail);

		private static string _spoken;

		private static void Arm() {
			SpeechPipeline.Reset();
			LineReview.Reset();
			_spoken = null;
			SpeechPipeline.SpeakAction = (text, interrupt) => _spoken = text;
			// Monotonic time so the interrupt dedup never swallows a re-spoken edge
			// line; each Step that actually speaks must land in _spoken.
			float t = 0f;
			SpeechPipeline.TimeSource = () => { t += 1f; return t; };
		}

		private const string ABC = "A. B. C.";
		// Two arbitrary, distinct focus keys for the stepping tests.
		private const string K1 = "item-1";
		private const string K2 = "item-2";
		private static readonly string NothingToReview =
			(string)STRINGS.ONIACCESS.HELP.NOTHING_TO_REVIEW;

		// ========================================
		// Segmentation (announcement -> lines)
		// ========================================

		private static (string, bool, string) SegmentSplitsOnSentence() {
			// The Vitals morale cell shape: parts joined with ". ".
			var lines = LineReview.Segment("Mira, Morale, 7. Expectations: -5. Decor: +3.");
			bool ok = lines.Count == 3
				&& lines[0] == "Mira, Morale, 7."
				&& lines[1] == "Expectations: -5."
				&& lines[2] == "Decor: +3.";
			return Check("SegmentSplitsOnSentence", ok, string.Join(" | ", lines));
		}

		private static (string, bool, string) SegmentSplitsOnRawNewline() {
			// A newline survives as a line boundary because the split runs before
			// TextFilter would collapse it to a space.
			var lines = LineReview.Segment("First line\nSecond line\nThird line");
			bool ok = lines.Count == 3
				&& lines[0] == "First line"
				&& lines[1] == "Second line"
				&& lines[2] == "Third line";
			return Check("SegmentSplitsOnRawNewline", ok, string.Join(" | ", lines));
		}

		private static (string, bool, string) SegmentFiltersEachLine() {
			var lines = LineReview.Segment("<b>Bold</b> text. <color=#fff>Colored</color> text.");
			bool ok = lines.Count == 2
				&& lines[0] == "Bold text."
				&& lines[1] == "Colored text.";
			return Check("SegmentFiltersEachLine", ok, string.Join(" | ", lines));
		}

		private static (string, bool, string) SegmentDropsEmptyLines() {
			// A fragment that filters to nothing must not become a blank line.
			var lines = LineReview.Segment("Real line.\n<sprite name=unknownsprite>\n\nAnother line.");
			bool ok = lines.Count == 2
				&& lines[0] == "Real line."
				&& lines[1] == "Another line.";
			return Check("SegmentDropsEmptyLines", ok, string.Join(" | ", lines));
		}

		private static (string, bool, string) SegmentEmptyInputYieldsNoLines() {
			bool ok = LineReview.Segment(null).Count == 0
				&& LineReview.Segment("").Count == 0;
			return Check("SegmentEmptyInputYieldsNoLines", ok, "expected no lines");
		}

		// ========================================
		// Stepping / clamping
		// ========================================

		private static (string, bool, string) ForwardThenClamps() {
			Arm();
			LineReview.Step(ABC, K1, 1); string a = _spoken;
			LineReview.Step(ABC, K1, 1); string b = _spoken;
			LineReview.Step(ABC, K1, 1); string c = _spoken;
			LineReview.Step(ABC, K1, 1); string clamp = _spoken; // past the end
			bool ok = a == "A." && b == "B." && c == "C." && clamp == "C.";
			return Check("ForwardThenClamps", ok, $"{a},{b},{c},{clamp}");
		}

		private static (string, bool, string) FreshUpEntersFirstLine() {
			Arm();
			LineReview.Step(ABC, K1, -1); // fresh state: enter at first line, not silence
			bool ok = _spoken == "A.";
			return Check("FreshUpEntersFirstLine", ok, $"spoken=\"{_spoken}\"");
		}

		private static (string, bool, string) BackwardThenClamps() {
			Arm();
			LineReview.Step(ABC, K1, 1); LineReview.Step(ABC, K1, 1); LineReview.Step(ABC, K1, 1); // at C
			LineReview.Step(ABC, K1, -1); string b = _spoken;
			LineReview.Step(ABC, K1, -1); string a = _spoken;
			LineReview.Step(ABC, K1, -1); string clamp = _spoken; // before the start
			bool ok = b == "B." && a == "A." && clamp == "A.";
			return Check("BackwardThenClamps", ok, $"{b},{a},{clamp}");
		}

		private static (string, bool, string) SameFocusContinues() {
			Arm();
			LineReview.Step(ABC, K1, 1); string a = _spoken;
			LineReview.Step(ABC, K1, 1); string b = _spoken; // same focus: advances, not rewinds
			bool ok = a == "A." && b == "B.";
			return Check("SameFocusContinues", ok, $"{a},{b}");
		}

		private static (string, bool, string) NewFocusRewinds() {
			Arm();
			LineReview.Step(ABC, K1, 1); LineReview.Step(ABC, K1, 1); // at B of first item
			LineReview.Step("X. Y.", K2, 1); // a different item rewinds to its first line
			bool ok = _spoken == "X.";
			return Check("NewFocusRewinds", ok, $"spoken=\"{_spoken}\"");
		}

		private static (string, bool, string) LiveValueDoesNotRewind() {
			Arm();
			// Same focus, but its announced value ticks between presses. The cursor must
			// keep advancing instead of resetting to line 0 -- the bug the focus key fixes.
			LineReview.Step("A. B. C.", K1, 1); string a = _spoken;
			LineReview.Step("A. B. D.", K1, 1); string b = _spoken; // C changed to D
			LineReview.Step("A. B. E.", K1, 1); string c = _spoken; // D changed to E
			bool ok = a == "A." && b == "B." && c == "E.";
			return Check("LiveValueDoesNotRewind", ok, $"{a},{b},{c}");
		}

		private static (string, bool, string) IdenticalContentNewFocusRewinds() {
			Arm();
			// Two distinct items whose announcements happen to be byte-identical. Moving
			// between them must still rewind, since the focus key changed.
			LineReview.Step("A. B.", K1, 1); LineReview.Step("A. B.", K1, 1); // at B
			LineReview.Step("A. B.", K2, 1); // different item, same text -> rewind to A
			bool ok = _spoken == "A.";
			return Check("IdenticalContentNewFocusRewinds", ok, $"spoken=\"{_spoken}\"");
		}

		private static (string, bool, string) EmptyContentSaysNothing() {
			Arm();
			LineReview.Step(null, K1, 1); string n1 = _spoken;
			_spoken = null;
			LineReview.Step("", K1, -1); string n2 = _spoken;
			bool ok = n1 == NothingToReview && n2 == NothingToReview;
			return Check("EmptyContentSaysNothing", ok, $"\"{n1}\",\"{n2}\"");
		}

		private static (string, bool, string) AllMarkupContentSaysNothing() {
			Arm();
			// Non-empty input that filters away to nothing yields no lines, so the
			// reviewer reports nothing rather than speaking an empty string.
			LineReview.Step("<sprite name=unknownsprite>", K1, 1);
			bool ok = _spoken == NothingToReview;
			return Check("AllMarkupContentSaysNothing", ok, $"spoken=\"{_spoken}\"");
		}

		public static IEnumerable<(string, bool, string)> All() {
			yield return SegmentSplitsOnSentence();
			yield return SegmentSplitsOnRawNewline();
			yield return SegmentFiltersEachLine();
			yield return SegmentDropsEmptyLines();
			yield return SegmentEmptyInputYieldsNoLines();
			yield return ForwardThenClamps();
			yield return FreshUpEntersFirstLine();
			yield return BackwardThenClamps();
			yield return SameFocusContinues();
			yield return NewFocusRewinds();
			yield return LiveValueDoesNotRewind();
			yield return IdenticalContentNewFocusRewinds();
			yield return EmptyContentSaysNothing();
			yield return AllMarkupContentSaysNothing();
			// Restore the shared sinks the rest of the suite expects.
			SpeechPipeline.SpeakAction = (text, interrupt) => { };
			SpeechPipeline.TimeSource = () => 0f;
			SpeechPipeline.Reset();
		}
	}
}
