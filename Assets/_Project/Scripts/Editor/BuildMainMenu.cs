using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MonsterChase.UI;

namespace MonsterChase.EditorTools
{
    /// <summary>Generates Scenes/Menu.unity, and registers every scene in Build Settings.</summary>
    public static class BuildMainMenu
    {
        const string ScenePath = "Assets/_Project/Scenes/Menu.unity";
        const string TestbedPath = "Assets/_Project/Scenes/Testbed.unity";

        [MenuItem("MonsterChase/Build Main Menu Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            System.IO.Directory.CreateDirectory("Assets/_Project/Scenes");

            var camGo = new GameObject("MainCamera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.03f, 0.04f);
            cam.orthographic = true;
            camGo.AddComponent<AudioListener>();

            UiKit.NewCanvas("MenuCanvas", out var canvasGo);
            UiKit.EnsureEventSystem();

            var title = UiKit.NewText("Title", canvasGo.transform, 92, TextAnchor.MiddleLeft);
            title.text = "MONSTER CHASE";
            UiKit.Place(title.rectTransform, 0.12f, 0.75f, 0.74f, 0.86f);

            var tagline = UiKit.NewText("Tagline", canvasGo.transform, 24, TextAnchor.MiddleLeft);
            tagline.text = "it does not stay down";
            tagline.color = UiKit.Dim;
            UiKit.Place(tagline.rectTransform, 0.125f, 0.7f, 0.69f, 0.735f);

            // The menu rows live under their own object so the settings panel can hide
            // the lot with one SetActive instead of walking children.
            var rowsGo = new GameObject("Rows", typeof(RectTransform));
            rowsGo.transform.SetParent(canvasGo.transform, false);
            UiKit.Stretch(rowsGo.GetComponent<RectTransform>());

            var play = UiKit.NewMenuButton("Play", rowsGo.transform, "play", 34, out _);
            UiKit.Place((RectTransform)play.transform, 0.12f, 0.45f, 0.52f, 0.58f);

            var controls = UiKit.NewMenuButton("Controls", rowsGo.transform, "controls", 34, out _);
            UiKit.Place((RectTransform)controls.transform, 0.12f, 0.45f, 0.45f, 0.51f);

            var quit = UiKit.NewMenuButton("Quit", rowsGo.transform, "quit", 34, out _);
            UiKit.Place((RectTransform)quit.transform, 0.12f, 0.45f, 0.38f, 0.44f);

            var settings = SettingsPanelBuilder.Build(canvasGo.transform);
            settings.transform.SetAsLastSibling();

            var menu = canvasGo.AddComponent<MainMenu>();
            var so = new SerializedObject(menu);
            so.FindProperty("rootPanel").objectReferenceValue = rowsGo;
            so.FindProperty("settings").objectReferenceValue = settings;
            so.FindProperty("playButton").objectReferenceValue = play;
            so.FindProperty("controlsButton").objectReferenceValue = controls;
            so.FindProperty("quitButton").objectReferenceValue = quit;
            so.FindProperty("playScene").stringValue = "Testbed";
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterScenes();

            Debug.Log($"[Menu] Built {ScenePath}. Build Settings lists " +
                      $"{EditorBuildSettings.scenes.Length} scenes.");
        }

        /// <summary>LoadScene fails, and fails quietly, on anything not in this list.</summary>
        public static void RegisterScenes()
        {
            var wanted = new[] { ScenePath, TestbedPath };
            var scenes = new List<EditorBuildSettingsScene>();
            foreach (var path in wanted)
                if (System.IO.File.Exists(path))
                    scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
