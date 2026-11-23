using System;
using NetLab.Models;
using UnityEngine.UIElements;

namespace NetLab.Components
{
    public sealed class DeviceNodeView
    {
        public DeviceModel Model { get; }
        public Button Root { get; }

        public event Action<DeviceModel> Selected;

        public DeviceNodeView(VisualTreeAsset template, DeviceModel model)
        {
            Model = model;
            Root = template.Instantiate().Q<Button>("device-node");
            Root.name = $"device-node-{model.Id}";
            Root.Q<Label>("device-glyph").text = GlyphFor(model.Type);
            Root.Q<Label>("device-name").text = model.DisplayName;
            Root.Q<Label>("device-subtitle").text = model.Subtitle;
            Root.clicked += () => Selected?.Invoke(Model);
        }

        public void SetSelected(bool selected)
        {
            Root.EnableInClassList("selected", selected);
        }

        private static string GlyphFor(string type)
        {
            return type switch
            {
                "Router" => "▤",
                "Switch" => "⊞",
                "Server" => "▥",
                _ => "▱"
            };
        }
    }
}
