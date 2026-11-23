using System;
using System.Collections.Generic;
using System.Linq;
using NetLab.Components;
using NetLab.Models;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace NetLab.Controllers
{
    public sealed class TopologyController : IDisposable
    {
        private sealed class PacketState
        {
            public Transform Transform;
            public string[] DevicePath;
            public float Duration;
            public float Delay;
        }

        private readonly VisualElement _root;
        private readonly VisualElement _viewport;
        private readonly VisualElement _nodeLayer;
        private readonly VisualElement _worldRender;
        private readonly Label _zoomLabel;
        private readonly Button _simulationButton;
        private readonly Label _simulationIcon;
        private readonly ToastController _toast;
        private List<DeviceModel> _devices;
        private List<ConnectionModel> _connections;
        private readonly Dictionary<string, DeviceNodeView> _nodeViews = new();
        private readonly Dictionary<string, Transform> _deviceObjects = new();
        private readonly Dictionary<string, Material> _deviceMaterials = new();
        private readonly List<PacketState> _packets = new();
        private readonly List<Button> _tools = new();

        private GameObject _worldRoot;
        private Camera _camera;
        private RenderTexture _renderTexture;
        private IVisualElementScheduledItem _animation;
        private VisualTreeAsset _deviceNodeTemplate;
        private bool _simulationRunning = true;
        private float _simulationTime;
        private int _zoom = 100;
        private string _selectedDeviceId = "router-01";

        public event Action<DeviceModel> DeviceSelected;

        public bool HasUnsavedChanges { get; private set; }
        public List<DeviceModel> Devices => _devices;
        public List<ConnectionModel> Connections => _connections;

        public TopologyController(
            VisualElement root,
            VisualTreeAsset deviceNodeTemplate,
            List<DeviceModel> devices,
            List<ConnectionModel> connections,
            ToastController toast)
        {
            _root = root;
            _viewport = root.Q("topology-viewport");
            _nodeLayer = root.Q("network-content");
            _worldRender = root.Q("world-render");
            _zoomLabel = root.Q<Label>("zoom-level");
            _simulationButton = root.Q<Button>("simulation-button");
            _simulationIcon = root.Q<Label>("simulation-icon");
            _toast = toast;
            _devices = devices;
            _connections = connections;
            _deviceNodeTemplate = deviceNodeTemplate;

            CreateWorld();
            CreateNodeOverlays(deviceNodeTemplate);
            BindControls();
            _viewport.RegisterCallback<GeometryChangedEvent>(OnViewportGeometryChanged);
            _animation = _viewport.schedule.Execute(UpdateAnimation).Every(16);
            RefreshSimulationUi();
            SetSelected(_selectedDeviceId);
        }

        private void CreateWorld()
        {
            _worldRoot = new GameObject("NetLab Runtime Topology");

            var cameraObject = new GameObject("Topology Camera");
            cameraObject.transform.SetParent(_worldRoot.transform);
            cameraObject.transform.position = new Vector3(0, 0, -10);
            _camera = cameraObject.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = 4.2f;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color32(21, 27, 37, 255);
            _camera.nearClipPlane = 0.1f;
            _camera.farClipPlane = 30;

            _renderTexture = new RenderTexture(3000, 3000, 24, RenderTextureFormat.ARGB32)
            {
                name = "NetLab Topology Render Texture",
                antiAliasing = 4
            };
            _renderTexture.Create();
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color32(21, 27, 37, 0);
            _camera.targetTexture = _renderTexture;
            if (_worldRender != null)
            {
                _worldRender.style.backgroundImage =
                    new StyleBackground(Background.FromRenderTexture(_renderTexture));
            }

            foreach (var device in _devices)
                CreateDeviceObject(device);
            foreach (var connection in _connections)
                CreateConnectionObject(connection);

            CreatePacket(
                "Primary Packet",
                new[] { "pc-01", "router-01", "switch-01", "server-01" },
                4f,
                0f);
            CreatePacket(
                "Secondary Packet",
                new[] { "router-01", "pc-02" },
                2.5f,
                0.8f);
        }

        private void CreateDeviceObject(DeviceModel device)
        {
            var primitive = device.Type switch
            {
                "Router" => PrimitiveType.Cylinder,
                "Server" => PrimitiveType.Cube,
                "Switch" => PrimitiveType.Cube,
                _ => PrimitiveType.Cube
            };
            var instance = GameObject.CreatePrimitive(primitive);
            instance.name = device.DisplayName;
            instance.transform.SetParent(_worldRoot.transform);
            instance.transform.position = WorldPosition(device);
            instance.transform.localScale = device.Type switch
            {
                "Router" => new Vector3(0.75f, 0.22f, 0.75f),
                "Switch" => new Vector3(0.9f, 0.23f, 0.55f),
                "Server" => new Vector3(0.5f, 0.8f, 0.42f),
                _ => new Vector3(0.62f, 0.45f, 0.22f)
            };
            if (device.Type == "Router")
                instance.transform.rotation = Quaternion.Euler(90, 0, 0);
            var material = CreateMaterial(device.Accent);
            instance.GetComponent<Renderer>().sharedMaterial = material;
            _deviceObjects[device.Id] = instance.transform;
            _deviceMaterials[device.Id] = material;
        }

        private void CreateConnectionObject(ConnectionModel connection)
        {
            if (!_deviceObjects.ContainsKey(connection.SourceDeviceId) || !_deviceObjects.ContainsKey(connection.TargetDeviceId))
                return;
            var start = _deviceObjects[connection.SourceDeviceId].position;
            var end = _deviceObjects[connection.TargetDeviceId].position;
            var control = (start + end) * 0.5f + connection.ControlOffset;
            var lineObject = new GameObject(connection.Id);
            lineObject.transform.SetParent(_worldRoot.transform);
            var line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 25;
            line.startWidth = 0.055f;
            line.endWidth = 0.055f;
            line.numCornerVertices = 4;
            line.sharedMaterial = CreateMaterial(connection.Color, true);
            for (var i = 0; i < line.positionCount; i++)
            {
                var t = i / (line.positionCount - 1f);
                line.SetPosition(i, Quadratic(start, control, end, t) + Vector3.forward * 0.2f);
            }
        }

        private void CreatePacket(string name, string[] path, float duration, float delay)
        {
            var packet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            packet.name = name;
            packet.transform.SetParent(_worldRoot.transform);
            packet.transform.localScale = Vector3.one * 0.16f;
            packet.GetComponent<Renderer>().sharedMaterial =
                CreateMaterial(new Color32(184, 255, 250, 255), true);
            _packets.Add(new PacketState
            {
                Transform = packet.transform,
                DevicePath = path,
                Duration = duration,
                Delay = delay
            });
        }

        private void CreateNodeOverlays(VisualTreeAsset template)
        {
            foreach (var device in _devices)
            {
                var view = new DeviceNodeView(template, device);
                view.Root.style.left = Length.Percent(device.NormalizedPosition.x * 100);
                view.Root.style.top = Length.Percent((1 - device.NormalizedPosition.y) * 100);
                view.Selected += selected =>
                {
                    SetSelected(selected.Id);
                    DeviceSelected?.Invoke(selected);
                    _toast.Show($"{selected.DisplayName} selected");
                };
                _nodeLayer.Add(view.Root);
                _nodeViews[device.Id] = view;
            }
        }

        private void BindControls()
        {
            _root.Q<Button>("zoom-in").clicked += () => SetZoom(Math.Min(150, _zoom + 10));
            _root.Q<Button>("zoom-out").clicked += () => SetZoom(Math.Max(60, _zoom - 10));
            _root.Q<Button>("center-view").clicked += () =>
            {
                SetZoom(100);
                _camera.transform.position = new Vector3(0, 0, -10);
                _toast.Show("Camera centered");
            };
            _simulationButton.clicked += () =>
            {
                _simulationRunning = !_simulationRunning;
                RefreshSimulationUi();
            };

            BindTool("tool-select", "Select tool active");
            BindTool("tool-connect", "Connection tool active — select two devices");
            BindTool("tool-layers", "Topology layers toggled");
            SelectTool(_root.Q<Button>("tool-select"), null);
        }

        private void BindTool(string id, string message)
        {
            var button = _root.Q<Button>(id);
            _tools.Add(button);
            button.clicked += () => SelectTool(button, message);
        }

        private void SelectTool(Button selected, string message)
        {
            foreach (var tool in _tools)
                tool.EnableInClassList("active", tool == selected);
            if (!string.IsNullOrEmpty(message))
                _toast.Show(message);
        }

        private void SetSelected(string deviceId, bool updateMaterial = true)
        {
            _selectedDeviceId = deviceId;
            foreach (var item in _nodeViews)
                item.Value.SetSelected(item.Key == deviceId);
            if (!updateMaterial)
                return;
            foreach (var item in _deviceMaterials)
            {
                var model = _devices.First(device => device.Id == item.Key);
                item.Value.color = item.Key == deviceId
                    ? Color.Lerp(model.Accent, Color.white, 0.35f)
                    : model.Accent;
            }
        }

        private void SetZoom(int zoom)
        {
            _zoom = zoom;
            _zoomLabel.text = $"{_zoom}%";
            _camera.orthographicSize = 4.2f * 100f / _zoom;
        }

        private void RefreshSimulationUi()
        {
            _simulationIcon.text = _simulationRunning ? "Ⅱ" : "▶";
            _simulationButton.Q<Label>("simulation-label").text =
                _simulationRunning ? "Pause simulation" : "Run simulation";
            _simulationButton.EnableInClassList("running", _simulationRunning);
        }

        private void UpdateAnimation()
        {
            _camera.backgroundColor = new Color32(21, 27, 37, 0);
            if (!_simulationRunning)
                return;
            _simulationTime += 0.016f;
            foreach (var packet in _packets)
            {
                if (_simulationTime < packet.Delay)
                {
                    packet.Transform.gameObject.SetActive(false);
                    continue;
                }
                packet.Transform.gameObject.SetActive(true);
                var elapsed = Mathf.Repeat(_simulationTime - packet.Delay, packet.Duration);
                var progress = elapsed / packet.Duration;
                var segmentCount = packet.DevicePath.Length - 1;
                var scaled = progress * segmentCount;
                var segment = Mathf.Min(Mathf.FloorToInt(scaled), segmentCount - 1);
                var local = scaled - segment;
                string sourceId = packet.DevicePath[segment];
                string targetId = packet.DevicePath[segment + 1];

                if (!_deviceObjects.ContainsKey(sourceId) || !_deviceObjects.ContainsKey(targetId))
                    continue;
                var start = _deviceObjects[sourceId].position;
                var end = _deviceObjects[targetId].position;
                var connection = FindConnection(sourceId, targetId);
                var control = (start + end) * 0.5f + (connection?.ControlOffset ?? Vector3.zero);
                packet.Transform.position = Quadratic(start, control, end, local) + Vector3.back * 0.1f;
            }
        }

        private ConnectionModel FindConnection(string source, string target)
        {
            return _connections.FirstOrDefault(connection =>
                (connection.SourceDeviceId == source && connection.TargetDeviceId == target) ||
                (connection.SourceDeviceId == target && connection.TargetDeviceId == source));
        }

        private void OnViewportGeometryChanged(GeometryChangedEvent evt)
        {
            // The topology remains at a fixed 3000px workspace size so its
            // elements and the grid keep their positions while scrolling.
        }

        private static Vector3 WorldPosition(DeviceModel device)
        {
            return new Vector3(
                (device.NormalizedPosition.x - 0.5f) * 11f,
                (device.NormalizedPosition.y - 0.5f) * 7f,
                0);
        }

        private static Vector3 Quadratic(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            var inverse = 1f - t;
            return inverse * inverse * a + 2f * inverse * t * b + t * t * c;
        }

        private static Material CreateMaterial(Color color, bool unlit = false)
        {
            var shader = Shader.Find(unlit ? "Unlit/Color" : "Universal Render Pipeline/Lit");
            shader ??= Shader.Find("Standard");
            shader ??= Shader.Find("Sprites/Default");
            var material = new Material(shader) { color = color };
            return material;
        }

        public void MarkDirty()
        {
            HasUnsavedChanges = true;
        }

        public void ReplaceTopology(
            List<DeviceModel> devices,
            List<ConnectionModel> connections)
        {
            RebuildTopology(devices, connections);
            HasUnsavedChanges = false;
        }

        public void ClearTopology()
        {
            RebuildTopology(new List<DeviceModel>(), new List<ConnectionModel>());
            HasUnsavedChanges = false;
        }

        public void MarkSaved()
        {
            HasUnsavedChanges = false;
        }

        private void RebuildTopology(
            List<DeviceModel> devices,
            List<ConnectionModel> connections)
        {
            _animation?.Pause();
            if (_camera != null)
                _camera.targetTexture = null;
            if (_worldRender != null)
                _worldRender.style.backgroundImage = StyleKeyword.None;
            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Object.Destroy(_renderTexture);
            }
            if (_worldRoot != null)
                Object.Destroy(_worldRoot);

            foreach (var item in _nodeViews)
                item.Value.Root.RemoveFromHierarchy();

            _nodeViews.Clear();
            _deviceObjects.Clear();
            _deviceMaterials.Clear();
            _packets.Clear();
            _devices = devices;
            _connections = connections;
            _simulationTime = 0;

            CreateWorld();
            CreateNodeOverlays(_deviceNodeTemplate);
            if (_devices.Count > 0)
            {
                _selectedDeviceId = _devices[0].Id;
                SetSelected(_selectedDeviceId);
            }
            _animation = _viewport.schedule.Execute(UpdateAnimation).Every(16);
        }

        public void Dispose()
        {
            _animation?.Pause();
            _viewport.UnregisterCallback<GeometryChangedEvent>(OnViewportGeometryChanged);
            if (_camera != null)
                _camera.targetTexture = null;
            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Object.Destroy(_renderTexture);
            }
            if (_worldRoot != null)
                Object.Destroy(_worldRoot);
        }
    }
}
