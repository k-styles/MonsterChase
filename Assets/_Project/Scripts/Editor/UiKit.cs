using UnityEngine;
using UnityEngine.UI;

namespace MonsterChase.EditorTools
{
    /// <summary>
    /// Builds the uGUI widgets every generated scene needs. One copy, because two
    /// scenes hand-rolling their own sliders is how a menu ends up looking different
    /// from the pause screen that is supposed to be the same menu.
    /// </summary>
    public static class UiKit
    {
        public static readonly Color Ink   = new Color(0.88f, 0.88f, 0.91f);
        public static readonly Color Dim   = new Color(0.55f, 0.55f, 0.59f);
        public static readonly Color Track = new Color(1f, 1f, 1f, 0.14f);
        public static readonly Color Fill  = new Color(0.78f, 0.24f, 0.22f);

        public static Canvas NewCanvas(string name, out GameObject go)
        {
            go = new GameObject(name, typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        /// <summary>
        /// This project is Input System only (activeInputHandler: 1), so the EventSystem
        /// must use InputSystemUIInputModule. StandaloneInputModule reads the legacy
        /// UnityEngine.Input class, which throws every frame under that setting and
        /// leaves every button in the game unclickable.
        /// </summary>
        public static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;

            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();

            var module = es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            // Without actions assigned the module is inert, and adding it from an editor
            // script does not fill them in the way the Inspector's Add Component does.
            module.AssignDefaultActions();
        }

        public static Text NewText(string name, Transform parent, int size, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size;
            t.alignment = anchor;
            t.color = Ink;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        public static Image NewImage(string name, Transform parent, Color c)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = c;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>A text row that highlights on hover. Returns the button; label is an out.</summary>
        public static Button NewMenuButton(string name, Transform parent, string label,
                                           int size, out Text text)
        {
            var hit = NewImage(name, parent, new Color(1f, 1f, 1f, 0f));
            hit.raycastTarget = true;

            text = NewText(name + "Label", hit.transform, size, TextAnchor.MiddleLeft);
            text.text = label;
            text.color = Dim;
            Stretch(text.rectTransform);

            var button = hit.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.ColorTint;
            var c = button.colors;
            c.normalColor      = new Color(1f, 1f, 1f, 0f);
            c.highlightedColor = new Color(1f, 1f, 1f, 0.08f);
            c.pressedColor     = new Color(1f, 1f, 1f, 0.16f);
            c.selectedColor    = new Color(1f, 1f, 1f, 0.08f);
            c.fadeDuration     = 0.15f;
            button.colors = c;
            return button;
        }

        /// <summary>
        /// A labelled slider row: name on the left, value on the right, track beneath.
        /// </summary>
        public static Slider NewSliderRow(string name, Transform parent, string label,
                                          out Text valueText)
        {
            var row = NewImage(name, parent, new Color(1f, 1f, 1f, 0f));

            var caption = NewText(name + "Caption", row.transform, 22, TextAnchor.UpperLeft);
            caption.text = label;
            caption.color = Ink;
            var cr = caption.rectTransform;
            cr.anchorMin = new Vector2(0f, 0.45f); cr.anchorMax = new Vector2(0.6f, 1f);
            cr.offsetMin = cr.offsetMax = Vector2.zero;

            valueText = NewText(name + "Value", row.transform, 22, TextAnchor.UpperRight);
            valueText.color = Dim;
            var vr = valueText.rectTransform;
            vr.anchorMin = new Vector2(0.6f, 0.45f); vr.anchorMax = new Vector2(1f, 1f);
            vr.offsetMin = vr.offsetMax = Vector2.zero;

            var sliderGo = new GameObject(name + "Slider", typeof(RectTransform));
            sliderGo.transform.SetParent(row.transform, false);
            var sr = sliderGo.GetComponent<RectTransform>();
            sr.anchorMin = new Vector2(0f, 0.05f); sr.anchorMax = new Vector2(1f, 0.38f);
            sr.offsetMin = sr.offsetMax = Vector2.zero;

            var track = NewImage("Track", sliderGo.transform, Track);
            track.raycastTarget = true;
            Stretch(track.rectTransform);

            var fillArea = new GameObject("FillArea", typeof(RectTransform));
            fillArea.transform.SetParent(sliderGo.transform, false);
            Stretch(fillArea.GetComponent<RectTransform>());
            var fill = NewImage("Fill", fillArea.transform, Fill);
            Stretch(fill.rectTransform);

            var slider = sliderGo.AddComponent<Slider>();
            slider.targetGraphic = track;
            slider.fillRect = fill.rectTransform;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            return slider;
        }

        /// <summary>A two-state row used for invert-Y and hold-to-crouch.</summary>
        public static Button NewToggleRow(string name, Transform parent, string label,
                                          out Text valueText)
        {
            var button = NewMenuButton(name, parent, label, 22, out var caption);
            caption.color = Ink;

            var cr = caption.rectTransform;
            cr.anchorMin = new Vector2(0f, 0f); cr.anchorMax = new Vector2(0.6f, 1f);
            cr.offsetMin = cr.offsetMax = Vector2.zero;

            valueText = NewText(name + "Value", button.transform, 22, TextAnchor.MiddleRight);
            valueText.color = Dim;
            var vr = valueText.rectTransform;
            vr.anchorMin = new Vector2(0.6f, 0f); vr.anchorMax = new Vector2(1f, 1f);
            vr.offsetMin = vr.offsetMax = Vector2.zero;
            return button;
        }

        public static void Place(RectTransform r, float xMin, float xMax, float yMin, float yMax)
        {
            r.anchorMin = new Vector2(xMin, yMin);
            r.anchorMax = new Vector2(xMax, yMax);
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        public static void Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }
    }
}
