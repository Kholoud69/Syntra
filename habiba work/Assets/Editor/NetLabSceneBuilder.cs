#if UNITY_EDITOR
using NetLab;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace NetLab.Editor
{
    public static class NetLabSceneBuilder
    {
        private const string Root = "Assets";
        private const string MainUxml = Root + "/UI/UXML/NetLab.uxml";
        private const string DeviceNodeUxml = Root + "/UI/UXML/DeviceNode.uxml";
        private const string InterfaceRowUxml = Root + "/UI/UXML/InterfaceRow.uxml";
        private const string PanelSettingsPath = Root + "/UI/NetLabPanelSettings.asset";
        private const string PrefabPath = Root + "/Prefabs/NetLabApp.prefab";
        private const string ScenePath = Root + "/Scenes/NetLab.unity";

        [MenuItem("Tools/NetLab/Create or Refresh NetLab Scene")]
        public static void CreateScene()
        {
            EnsureFolder(Root, "Scenes");
            EnsureFolder(Root, "Prefabs");
            EnsureFolder(Root + "/UI", "UXML");

            var mainLayout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MainUxml);
            var deviceNode = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(DeviceNodeUxml);
            var interfaceRow = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(InterfaceRowUxml);
            if (mainLayout == null || deviceNode == null || interfaceRow == null)
            {
                Debug.LogError(
                    "NetLab UI assets were not found. Copy the UnityExport/Assets contents " +
                    "into the Unity project's Assets folder before running setup.");
                return;
            }

            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (panelSettings == null)
            {
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                panelSettings.name = "NetLab Panel Settings";
                panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                panelSettings.referenceResolution = new Vector2Int(1440, 900);
                panelSettings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
                panelSettings.match = 0.5f;
                AssetDatabase.CreateAsset(panelSettings, PanelSettingsPath);
            }

            var appObject = new GameObject("NetLab Application");
            var document = appObject.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.visualTreeAsset = mainLayout;
            document.sortingOrder = 0;

            var application = appObject.AddComponent<NetLabApplication>();
            var serialized = new SerializedObject(application);
            serialized.FindProperty("deviceNodeTemplate").objectReferenceValue = deviceNode;
            serialized.FindProperty("interfaceRowTemplate").objectReferenceValue = interfaceRow;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(appObject, PrefabPath);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Object.DestroyImmediate(appObject);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            PrefabUtility.InstantiatePrefab(prefab, scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            Debug.Log("NetLab scene created at Assets/Scenes/NetLab.unity.");
        }

        private static void EnsureFolder(string parent, string child)
        {
            var path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }
    }
}
#endif
