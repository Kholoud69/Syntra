using UnityEngine.UIElements;

namespace NetLab.Controllers
{
    public sealed class DeviceLibraryController
    {
        public DeviceLibraryController(VisualElement root, ToastController toast)
        {
            if (root == null) return;

            Bind(root, toast, "library-router", "Router");
            Bind(root, toast, "library-switch", "Switch");
            Bind(root, toast, "library-pc", "PC");
            Bind(root, toast, "library-server", "Server");

            var searchBtn = root.Q<Button>("device-search");
            if (searchBtn != null && toast != null)
            {
                searchBtn.clicked += () => toast.Show("Device search ready");
            }
        }

        private static void Bind(VisualElement root, ToastController toast, string id, string label)
        {
            var button = root.Q<Button>(id);
            if (button != null && toast != null)
            {
                button.clicked += () => toast.Show($"{label} added to the device tray");
            }
        }
    }
}