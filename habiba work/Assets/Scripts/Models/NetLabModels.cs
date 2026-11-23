using System;
using System.Collections.Generic;
using UnityEngine;

namespace NetLab.Models
{
    [Serializable]
    public sealed class NetworkInterfaceModel
    {
        public string Id;
        public string Name;
        public string Speed;
        public string IpAddress;
        public string SubnetMask;
        public bool IsUp;
    }

    [Serializable]
    public sealed class RouteModel
    {
        public string Destination;
        public string NextHop;
        public int AdministrativeDistance = 1;
    }

    [Serializable]
    public sealed class DeviceModel
    {
        public string Id;
        public string DisplayName;
        public string Type;
        public string Subtitle;
        public string Model;
        public string OperatingSystem;
        public string DefaultGateway;
        public Vector2 NormalizedPosition;
        public Color Accent;
        public List<NetworkInterfaceModel> Interfaces = new();
        public List<RouteModel> Routes = new();
    }

    [Serializable]
    public sealed class ConnectionModel
    {
        public string Id;
        public string SourceDeviceId;
        public string TargetDeviceId;
        public Color Color;
        public Vector3 ControlOffset;
    }

    public enum ChatRole
    {
        Ai,
        User
    }

    [Serializable]
    public sealed class ChatMessageModel
    {
        public ChatRole Role;
        [TextArea] public string Text;

        public ChatMessageModel(ChatRole role, string text)
        {
            Role = role;
            Text = text;
        }
    }

    public static class NetLabSeedData
    {
        public static List<DeviceModel> CreateDevices()
        {
            var router = CreateDevice(
                "router-01", "Router-01", "Router", "Core Gateway",
                new Vector2(0.37f, 0.56f), new Color32(119, 133, 255, 255));
            router.Model = "ISR 4331";
            router.OperatingSystem = "IOS 16.9";
            router.DefaultGateway = "10.0.1.254";
            router.Interfaces.Add(new NetworkInterfaceModel
            {
                Id = "g0",
                Name = "GigabitEthernet0/0",
                Speed = "1 Gbps",
                IpAddress = "10.0.1.1",
                SubnetMask = "255.255.255.0",
                IsUp = true
            });
            router.Interfaces.Add(new NetworkInterfaceModel
            {
                Id = "g1",
                Name = "GigabitEthernet0/1",
                Speed = "1 Gbps",
                IpAddress = "10.0.2.1",
                SubnetMask = "255.255.255.0",
                IsUp = true
            });
            router.Routes.Add(new RouteModel
            {
                Destination = "10.0.2.0/24",
                NextHop = "10.0.1.2",
                AdministrativeDistance = 1
            });

            return new List<DeviceModel>
            {
                CreateDevice("pc-01", "PC-01", "PC", "192.168.1.10",
                    new Vector2(0.18f, 0.40f), new Color32(39, 167, 232, 255)),
                router,
                CreateDevice("switch-01", "Switch-01", "Switch", "Access Layer",
                    new Vector2(0.63f, 0.57f), new Color32(63, 191, 186, 255)),
                CreateDevice("server-01", "Server-01", "Server", "10.0.2.15",
                    new Vector2(0.84f, 0.35f), new Color32(165, 112, 244, 255)),
                CreateDevice("pc-02", "PC-02", "PC", "192.168.2.12",
                    new Vector2(0.55f, 0.26f), new Color32(39, 167, 232, 255))
            };
        }

        public static List<ConnectionModel> CreateConnections()
        {
            return new List<ConnectionModel>
            {
                Connection("pc-router", "pc-01", "router-01", new Color32(88, 108, 241, 255), new Vector3(0, 0.45f, 0)),
                Connection("router-switch", "router-01", "switch-01", new Color32(88, 108, 241, 255), new Vector3(0, 0.65f, 0)),
                Connection("switch-server", "switch-01", "server-01", new Color32(88, 108, 241, 255), new Vector3(0, 0.45f, 0)),
                Connection("router-pc02", "router-01", "pc-02", new Color32(53, 175, 169, 255), new Vector3(0, -0.8f, 0))
            };
        }

        private static DeviceModel CreateDevice(
            string id, string name, string type, string subtitle, Vector2 position, Color accent)
        {
            return new DeviceModel
            {
                Id = id,
                DisplayName = name,
                Type = type,
                Subtitle = subtitle,
                Model = "ISR 4331",
                OperatingSystem = "IOS 16.9",
                NormalizedPosition = position,
                Accent = accent
            };
        }

        private static ConnectionModel Connection(
            string id, string source, string target, Color color, Vector3 controlOffset)
        {
            return new ConnectionModel
            {
                Id = id,
                SourceDeviceId = source,
                TargetDeviceId = target,
                Color = color,
                ControlOffset = controlOffset
            };
        }
    }
}
