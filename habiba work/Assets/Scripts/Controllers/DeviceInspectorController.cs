using System.Collections.Generic;
using System.Linq;
using NetLab.Components;
using NetLab.Models;
using UnityEngine.UIElements;

namespace NetLab.Controllers
{
    public sealed class DeviceInspectorController
    {
        private readonly VisualElement _root;
        private readonly VisualTreeAsset _interfaceTemplate;
        private readonly ToastController _toast;
        private readonly List<DeviceModel> _devices;
        private readonly System.Action _changed;
        private DeviceModel _selected;

        public DeviceInspectorController(
            VisualElement root,
            VisualTreeAsset interfaceTemplate,
            List<DeviceModel> devices,
            ToastController toast,
            System.Action changed = null)
        {
            _root = root;
            _interfaceTemplate = interfaceTemplate;
            _devices = devices;
            _toast = toast;
            _changed = changed;

            BindTabs();
            root.Q<Button>("more-device-actions").clicked += () =>
                _toast.Show($"More actions for {_selected.DisplayName}");
            root.Q<Button>("add-route").clicked += AddRoute;
            root.Q<Button>("delete-route").clicked += DeleteRoute;
            root.Q<Button>("apply-route").clicked += ApplyRecommendedRoute;
            SelectDevice(_devices.First(device => device.Id == "router-01"));
        }

        public void SelectDevice(DeviceModel device)
        {
            _selected = device;
            _root.Q<Label>("selected-device-name").text = device.DisplayName;
            _root.Q<Label>("device-model").text = device.Model;
            _root.Q<Label>("device-os").text = device.OperatingSystem;

            var primary = device.Interfaces.FirstOrDefault();
            _root.Q<TextField>("ip-address").value = primary?.IpAddress ?? "10.0.1.1";
            _root.Q<TextField>("subnet-mask").value = primary?.SubnetMask ?? "255.255.255.0";
            _root.Q<TextField>("default-gateway").value =
                string.IsNullOrEmpty(device.DefaultGateway) ? "10.0.1.254" : device.DefaultGateway;

            var interfaceList = _root.Q("interface-list");
            interfaceList.Clear();
            var interfaces = device.Interfaces.Count > 0
                ? device.Interfaces
                : DefaultInterfaces();
            foreach (var item in interfaces)
            {
                var row = new InterfaceRowView(_interfaceTemplate, item);
                row.Changed += _ =>
                {
                    RefreshInterfaceCount(interfaces);
                    _changed?.Invoke();
                };
                interfaceList.Add(row.Root);
            }
            RefreshInterfaceCount(interfaces);
            RefreshRoute();
        }

        private void RefreshInterfaceCount(List<NetworkInterfaceModel> interfaces)
        {
            _root.Q<Label>("interface-count").text =
                $"{interfaces.Count(item => item.IsUp)} active";
        }

        private void BindTabs()
        {
            var tabs = new[]
            {
                _root.Q<Button>("tab-config"),
                _root.Q<Button>("tab-interfaces"),
                _root.Q<Button>("tab-routes")
            };
            foreach (var tab in tabs)
            {
                var captured = tab;
                tab.clicked += () =>
                {
                    foreach (var item in tabs)
                        item.EnableInClassList("active", item == captured);
                };
            }
            tabs[0].AddToClassList("active");
        }

        private void AddRoute()
        {
            EnsureRecommendedRoute();
            RefreshRoute();
            _changed?.Invoke();
            _toast.Show("Static route added");
        }

        private void DeleteRoute()
        {
            _selected.Routes.Clear();
            RefreshRoute();
            _changed?.Invoke();
            _toast.Show("Static route removed");
        }

        private void ApplyRecommendedRoute()
        {
            EnsureRecommendedRoute();
            RefreshRoute();
            _changed?.Invoke();
            _toast.Show("Route applied to Router-01");
        }

        private void EnsureRecommendedRoute()
        {
            if (_selected.Routes.Count == 0)
            {
                _selected.Routes.Add(new RouteModel
                {
                    Destination = "10.0.2.0/24",
                    NextHop = "10.0.1.2",
                    AdministrativeDistance = 1
                });
            }
        }

        private void RefreshRoute()
        {
            var route = _selected.Routes.FirstOrDefault();
            var row = _root.Q("route-row");
            var empty = _root.Q("empty-route");
            row.style.display = route == null ? DisplayStyle.None : DisplayStyle.Flex;
            empty.style.display = route == null ? DisplayStyle.Flex : DisplayStyle.None;
            if (route == null)
                return;
            _root.Q<Label>("route-destination").text = route.Destination;
            _root.Q<Label>("route-next-hop").text = $"via {route.NextHop}";
            _root.Q<Label>("route-metric").text = $"AD {route.AdministrativeDistance}";
        }

        private static List<NetworkInterfaceModel> DefaultInterfaces()
        {
            return new List<NetworkInterfaceModel>
            {
                new()
                {
                    Id = "g0",
                    Name = "GigabitEthernet0/0",
                    Speed = "1 Gbps",
                    IpAddress = "10.0.1.1",
                    SubnetMask = "255.255.255.0",
                    IsUp = true
                },
                new()
                {
                    Id = "g1",
                    Name = "GigabitEthernet0/1",
                    Speed = "1 Gbps",
                    IpAddress = "10.0.2.1",
                    SubnetMask = "255.255.255.0",
                    IsUp = true
                }
            };
        }
    }
}
