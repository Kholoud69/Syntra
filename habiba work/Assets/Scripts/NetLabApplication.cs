using System.Collections.Generic;
using NetLab.Controllers;
using NetLab.Models;
using UnityEngine;
using UnityEngine.UIElements;

namespace NetLab
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class NetLabApplication : MonoBehaviour
    {
        [Header("Reusable UI templates")]
        [SerializeField] private VisualTreeAsset deviceNodeTemplate;
        [SerializeField] private VisualTreeAsset interfaceRowTemplate;

        private TopologyController _topology;

        private void OnEnable()
        {
            var documentRoot = GetComponent<UIDocument>().rootVisualElement;
            var root = documentRoot.Q<VisualElement>("app");
            if (root == null)
            {
                Debug.LogError("NetLab.uxml is not assigned to the UIDocument.");
                return;
            }
            root.AddToClassList("netlab-root");

            var devices = NetLabSeedData.CreateDevices();
            var connections = NetLabSeedData.CreateConnections();
            var toast = new ToastController(root);
            _ = new ThemeController(root);
            _ = new ResponsiveController(root);
            _ = new NavigationController(root, toast);
            _ = new DeviceLibraryController(root, toast);
            var inspector = new DeviceInspectorController(
                root, interfaceRowTemplate, devices, toast,
                () => _topology?.MarkDirty());
            _ = new AiChatController(root, toast);
            _topology = new TopologyController(
                root, deviceNodeTemplate, devices, connections, toast);
            _ = new ProjectFileManager(root, toast, _topology);
            _topology.DeviceSelected += inspector.SelectDevice;
        }

        private void OnDisable()
        {
            if (_topology != null)
            {
                _topology.Dispose();
                _topology = null;
            }
        }
    }
}
