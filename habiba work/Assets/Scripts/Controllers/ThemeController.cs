using UnityEngine;
using UnityEngine.UIElements;

namespace NetLab.Controllers
{
    public sealed class ThemeController
    {
        private const string PreferenceKey = "NetLab.DarkTheme";
        private readonly VisualElement _root;
        private readonly Button _themeButton;
        private readonly VisualElement _sun;
        private readonly VisualElement _moon;
        private bool _dark;

        public ThemeController(VisualElement root)
        {
            _root = root;
            _themeButton = root.Q<Button>("theme-toggle");
            _sun = root.Q("theme-sun");
            _moon = root.Q("theme-moon");
            _dark = PlayerPrefs.GetInt(PreferenceKey, 1) == 1;
            _themeButton.clicked += Toggle;
            Apply();
        }

        private void Toggle()
        {
            _dark = !_dark;
            PlayerPrefs.SetInt(PreferenceKey, _dark ? 1 : 0);
            PlayerPrefs.Save();
            Apply();
        }

        private void Apply()
        {
            _root.EnableInClassList("theme-dark", _dark);
            _root.EnableInClassList("theme-light", !_dark);
            _sun.EnableInClassList("active", !_dark);
            _moon.EnableInClassList("active", _dark);
        }
    }
}
