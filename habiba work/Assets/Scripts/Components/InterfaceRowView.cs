using System;
using NetLab.Models;
using UnityEngine.UIElements;

namespace NetLab.Components
{
    public sealed class InterfaceRowView
    {
        public VisualElement Root { get; }
        public NetworkInterfaceModel Model { get; }

        public event Action<NetworkInterfaceModel> Changed;

        private readonly Label _status;
        private readonly Button _toggle;

        public InterfaceRowView(VisualTreeAsset template, NetworkInterfaceModel model)
        {
            Model = model;
            Root = template.Instantiate();
            Root.Q<Label>("interface-name").text = model.Name;
            _status = Root.Q<Label>("interface-status");
            _toggle = Root.Q<Button>("interface-toggle");
            _toggle.clicked += Toggle;
            Refresh();
        }

        private void Toggle()
        {
            Model.IsUp = !Model.IsUp;
            Refresh();
            Changed?.Invoke(Model);
        }

        private void Refresh()
        {
            _status.text = Model.IsUp ? $"Up · {Model.Speed}" : "Administratively down";
            _toggle.EnableInClassList("on", Model.IsUp);
        }
    }
}
