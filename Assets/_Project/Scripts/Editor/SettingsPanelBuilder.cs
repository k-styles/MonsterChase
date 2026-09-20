using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using MonsterChase.UI;

namespace MonsterChase.EditorTools
{
    /// <summary>
    /// Builds the options screen. Shared by the main menu and the pause screen so
    /// there is exactly one options layout in the game.
    /// </summary>
    public static class SettingsPanelBuilder
    {
        public static SettingsPanel Build(Transform parent, string title = "CONTROLS")
        {
            var panelGo = UiKit.NewImage("SettingsPanel", parent, new Color(0.04f, 0.04f, 0.05f, 0.92f));
            panelGo.raycastTarget = true;
            UiKit.Stretch(panelGo.rectTransform);

            var heading = UiKit.NewText("Heading", panelGo.transform, 46, TextAnchor.MiddleLeft);
            heading.text = title;
            UiKit.Place(heading.rectTransform, 0.14f, 0.7f, 0.85f, 0.93f);

            var panel = panelGo.gameObject.AddComponent<SettingsPanel>();
            var so = new SerializedObject(panel);

            // Rows top to bottom. Each is 0.075 tall with a 0.012 gap.
            float y = 0.74f;
            const float h = 0.075f, gap = 0.012f;

            var mouse = UiKit.NewSliderRow("MouseRow", panelGo.transform,
                "mouse sensitivity", out var mouseValue);
            UiKit.Place(((RectTransform)mouse.transform.parent), 0.14f, 0.86f, y - h, y); y -= h + gap;

            var arrow = UiKit.NewSliderRow("ArrowRow", panelGo.transform,
                "arrow key look speed", out var arrowValue);
            UiKit.Place(((RectTransform)arrow.transform.parent), 0.14f, 0.86f, y - h, y); y -= h + gap;

            var fov = UiKit.NewSliderRow("FovRow", panelGo.transform,
                "field of view", out var fovValue);
            UiKit.Place(((RectTransform)fov.transform.parent), 0.14f, 0.86f, y - h, y); y -= h + gap;

            var volume = UiKit.NewSliderRow("VolumeRow", panelGo.transform,
                "master volume", out var volumeValue);
            UiKit.Place(((RectTransform)volume.transform.parent), 0.14f, 0.86f, y - h, y); y -= h + gap;

            var invert = UiKit.NewToggleRow("InvertRow", panelGo.transform,
                "invert vertical look", out var invertValue);
            UiKit.Place((RectTransform)invert.transform, 0.14f, 0.86f, y - 0.05f, y); y -= 0.05f + gap;

            var crouch = UiKit.NewToggleRow("CrouchRow", panelGo.transform,
                "crouch", out var crouchValue);
            UiKit.Place((RectTransform)crouch.transform, 0.14f, 0.86f, y - 0.05f, y); y -= 0.05f + gap;

            var reset = UiKit.NewMenuButton("ResetRow", panelGo.transform,
                "reset to defaults", 22, out var resetLabel);
            resetLabel.color = UiKit.Dim;
            UiKit.Place((RectTransform)reset.transform, 0.14f, 0.5f, 0.13f, 0.18f);

            var back = UiKit.NewMenuButton("BackRow", panelGo.transform,
                "back  [esc]", 26, out var backLabel);
            UiKit.Place((RectTransform)back.transform, 0.14f, 0.5f, 0.06f, 0.12f);

            so.FindProperty("mouseSlider").objectReferenceValue = mouse;
            so.FindProperty("mouseValue").objectReferenceValue = mouseValue;
            so.FindProperty("arrowSlider").objectReferenceValue = arrow;
            so.FindProperty("arrowValue").objectReferenceValue = arrowValue;
            so.FindProperty("fovSlider").objectReferenceValue = fov;
            so.FindProperty("fovValue").objectReferenceValue = fovValue;
            so.FindProperty("volumeSlider").objectReferenceValue = volume;
            so.FindProperty("volumeValue").objectReferenceValue = volumeValue;
            so.FindProperty("invertButton").objectReferenceValue = invert;
            so.FindProperty("invertValue").objectReferenceValue = invertValue;
            so.FindProperty("crouchButton").objectReferenceValue = crouch;
            so.FindProperty("crouchValue").objectReferenceValue = crouchValue;
            so.FindProperty("resetButton").objectReferenceValue = reset;
            so.FindProperty("backButton").objectReferenceValue = back;
            so.ApplyModifiedPropertiesWithoutUndo();

            return panel;
        }
    }
}
