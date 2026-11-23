using System.Collections.Generic;
using UnityEngine.UIElements;

namespace NetLab.Controllers
{
    public sealed class NavigationController
    {
        private readonly List<Button> _buttons = new();
        private readonly ToastController _toast;

        public NavigationController(VisualElement root, ToastController toast)
        {
            _toast = toast;
            foreach (var name in new[] { "Workspace", "Labs", "Learn" })
            {
                var button = root.Q<Button>($"nav-{name.ToLowerInvariant()}");
                _buttons.Add(button);
                button.clicked += () => Select(button, name);
            }

            root.Q<Button>("settings-button").clicked += () => _toast.Show("Workspace settings opened");
            Select(_buttons[0], "Workspace", false);
        }

        private void Select(Button selected, string name, bool notify = true)
        {
            foreach (var button in _buttons)
                button.EnableInClassList("active", button == selected);
            if (notify)
                _toast.Show($"{name} view selected");
        }
    }
}
