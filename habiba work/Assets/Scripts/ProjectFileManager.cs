using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NetLab.Controllers;
using NetLab.Models;
using UnityEngine;
using UnityEngine.UIElements;

namespace NetLab
{
    [System.Serializable]
    public class DeviceData
    {
        public string deviceName;
        public string deviceType;
        public float posX;
        public float posY;
        public string id;
        public string subtitle;
        public string model;
        public string operatingSystem;
        public string defaultGateway;
        public bool hasNormalizedPosition;
        public float accentR;
        public float accentG;
        public float accentB;
        public float accentA;
        public List<NetworkInterfaceModel> interfaces = new List<NetworkInterfaceModel>();
        public List<RouteModel> routes = new List<RouteModel>();
    }

    [System.Serializable]
    public class ConnectionData
    {
        public string id;
        public string sourceDeviceId;
        public string targetDeviceId;
        public float colorR;
        public float colorG;
        public float colorB;
        public float colorA;
        public Vector3 controlOffset;
    }

    [System.Serializable]
    public class NetworkSaveData
    {
        public List<DeviceData> devices = new List<DeviceData>();
        public List<ConnectionData> connections = new List<ConnectionData>();
    }

    public sealed class ProjectFileManager
    {
        private readonly VisualElement _root;
        private readonly TopologyController _topology;
        private readonly ToastController _toast;
        private string _currentFilePath;

        public ProjectFileManager(
            VisualElement root,
            ToastController toast,
            TopologyController topology)
        {
            _root = root;
            _toast = toast;
            _topology = topology;

            var saveButton = root.Q<Button>("btn-save");
            var newButton = root.Q<Button>("btn-new");
            var openButton = root.Q<Button>("btn-open");

            if (saveButton != null)
                saveButton.clicked += SaveTopologyFile;
            if (newButton != null)
                newButton.clicked += ShowNewConfirmation;
            if (openButton != null)
                openButton.clicked += OpenTopologyFile;
        }

        private void SaveTopologyFile()
        {
            var path = _currentFilePath;
            if (string.IsNullOrEmpty(path))
            {
                path = ShowNativeSaveFilePanel(
                    "Save Network Project",
                    Application.dataPath,
                    "network_topology",
                    "json");
            }

            if (string.IsNullOrEmpty(path))
                return;

            var saveData = new NetworkSaveData();
            foreach (var device in _topology.Devices)
            {
                var data = new DeviceData
                {
                    id = device.Id,
                    deviceName = device.DisplayName,
                    deviceType = device.Type,
                    subtitle = device.Subtitle,
                    model = device.Model,
                    operatingSystem = device.OperatingSystem,
                    defaultGateway = device.DefaultGateway,
                    posX = device.NormalizedPosition.x,
                    posY = device.NormalizedPosition.y,
                    hasNormalizedPosition = true,
                    accentR = device.Accent.r,
                    accentG = device.Accent.g,
                    accentB = device.Accent.b,
                    accentA = device.Accent.a,
                    interfaces = device.Interfaces,
                    routes = device.Routes
                };
                saveData.devices.Add(data);
            }

            foreach (var connection in _topology.Connections)
            {
                saveData.connections.Add(new ConnectionData
                {
                    id = connection.Id,
                    sourceDeviceId = connection.SourceDeviceId,
                    targetDeviceId = connection.TargetDeviceId,
                    colorR = connection.Color.r,
                    colorG = connection.Color.g,
                    colorB = connection.Color.b,
                    colorA = connection.Color.a,
                    controlOffset = connection.ControlOffset
                });
            }

            try
            {
                File.WriteAllText(path, JsonUtility.ToJson(saveData, true));
                _currentFilePath = path;
                _topology.MarkSaved();
                _toast?.Show("Topology saved");
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"Failed to save topology: {exception.Message}");
                _toast?.Show("Could not save topology");
            }
        }

        private void OpenTopologyFile()
        {
            var path = ShowNativeOpenFilePanel(
                "Open Network Project",
                Application.dataPath,
                "json");

            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                var saveData = JsonUtility.FromJson<NetworkSaveData>(
                    File.ReadAllText(path));
                if (saveData == null)
                    throw new System.InvalidOperationException("The selected file is not valid topology JSON.");

                var devices = new List<DeviceModel>();
                foreach (var data in saveData.devices ?? new List<DeviceData>())
                {
                    var position = new Vector2(
                        data.hasNormalizedPosition ? data.posX : data.posX,
                        data.hasNormalizedPosition ? data.posY : data.posY);
                    if (!data.hasNormalizedPosition)
                    {
                        if (position.x > 1f)
                            position.x /= 3000f;
                        if (position.y > 1f)
                            position.y /= 3000f;
                    }
                    position.x = Mathf.Clamp01(position.x);
                    position.y = Mathf.Clamp01(position.y);

                    devices.Add(new DeviceModel
                    {
                        Id = string.IsNullOrEmpty(data.id) ? data.deviceName : data.id,
                        DisplayName = data.deviceName,
                        Type = data.deviceType,
                        Subtitle = data.subtitle,
                        Model = data.model,
                        OperatingSystem = data.operatingSystem,
                        DefaultGateway = data.defaultGateway,
                        NormalizedPosition = position,
                        Accent = new Color(
                            data.accentR <= 0f ? 0.46f : data.accentR,
                            data.accentG <= 0f ? 0.52f : data.accentG,
                            data.accentB <= 0f ? 1f : data.accentB,
                            data.accentA <= 0f ? 1f : data.accentA),
                        Interfaces = data.interfaces ?? new List<NetworkInterfaceModel>(),
                        Routes = data.routes ?? new List<RouteModel>()
                    });
                }

                var connections = new List<ConnectionModel>();
                foreach (var data in saveData.connections ?? new List<ConnectionData>())
                {
                    connections.Add(new ConnectionModel
                    {
                        Id = data.id,
                        SourceDeviceId = data.sourceDeviceId,
                        TargetDeviceId = data.targetDeviceId,
                        Color = new Color(
                            data.colorR,
                            data.colorG,
                            data.colorB,
                            data.colorA <= 0f ? 1f : data.colorA),
                        ControlOffset = data.controlOffset
                    });
                }

                _topology.ReplaceTopology(devices, connections);
                _currentFilePath = path;
                _toast?.Show("Topology opened");
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"Failed to open topology: {exception.Message}");
                _toast?.Show("Could not open topology");
            }
        }

        private static string ShowNativeSaveFilePanel(
            string title,
            string directory,
            string defaultName,
            string extension)
        {
            var type = FindFileBrowserType();
            if (type == null)
                return string.Empty;

            var method = type.GetMethod(
                "SaveFilePanel",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string), typeof(string), typeof(string), typeof(string) },
                null);
            return method?.Invoke(null, new object[] { title, directory, defaultName, extension }) as string
                ?? string.Empty;
        }

        private static string ShowNativeOpenFilePanel(
            string title,
            string directory,
            string extension)
        {
            var type = FindFileBrowserType();
            if (type == null)
                return string.Empty;

            var method = type.GetMethod(
                "OpenFilePanel",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(string), typeof(string), typeof(string), typeof(bool) },
                null);
            var result = method?.Invoke(
                null,
                new object[] { title, directory, extension, false }) as string[];
            return result != null && result.Length > 0 ? result[0] : string.Empty;
        }

        private static System.Type FindFileBrowserType()
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType("SFB.StandaloneFileBrowser");
                if (type != null)
                    return type;
            }
            return null;
        }

        private void ShowNewConfirmation()
        {
            if (!_topology.HasUnsavedChanges)
            {
                CreateNewTopology();
                return;
            }

            var overlay = new VisualElement();
            overlay.name = "confirmation-overlay";
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0;
            overlay.style.right = 0;
            overlay.style.top = 0;
            overlay.style.bottom = 0;
            overlay.style.backgroundColor = new Color(0, 0, 0, 0.6f);
            overlay.style.alignItems = Align.Center;
            overlay.style.justifyContent = Justify.Center;

            var dialog = new VisualElement();
            dialog.style.width = 350;
            dialog.style.height = 150;
            dialog.style.backgroundColor = new Color(0.15f, 0.18f, 0.24f);
            dialog.style.borderTopLeftRadius = 8;
            dialog.style.borderTopRightRadius = 8;
            dialog.style.borderBottomLeftRadius = 8;
            dialog.style.borderBottomRightRadius = 8;
            dialog.style.alignItems = Align.Center;
            dialog.style.justifyContent = Justify.SpaceAround;
            dialog.style.paddingTop = 15;
            dialog.style.paddingBottom = 15;

            var message = new Label(
                "Unsaved changes will be lost. Create a new topology?");
            message.style.color = Color.white;
            message.style.unityTextAlign = TextAnchor.MiddleCenter;
            message.style.whiteSpace = WhiteSpace.Normal;

            var buttons = new VisualElement();
            buttons.style.flexDirection = FlexDirection.Row;
            buttons.style.width = Length.Percent(100);
            buttons.style.justifyContent = Justify.SpaceEvenly;

            var yes = new Button(() =>
            {
                CreateNewTopology();
                _root.Remove(overlay);
            });
            yes.text = "Create New";

            var cancel = new Button(() => _root.Remove(overlay));
            cancel.text = "Cancel";

            buttons.Add(yes);
            buttons.Add(cancel);
            dialog.Add(message);
            dialog.Add(buttons);
            overlay.Add(dialog);
            _root.Add(overlay);
        }

        private void CreateNewTopology()
        {
            _topology.ClearTopology();
            _currentFilePath = null;
            _toast?.Show("New topology created");
        }
    }
}
