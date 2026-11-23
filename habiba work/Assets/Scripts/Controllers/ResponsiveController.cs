using UnityEngine.UIElements;

namespace NetLab.Controllers
{
    public sealed class ResponsiveController
    {
        private readonly VisualElement _root;

        public ResponsiveController(VisualElement root)
        {
            _root = root;
            _root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            _root.EnableInClassList("compact-width", evt.newRect.width <= 1050);
            _root.EnableInClassList("compact-height", evt.newRect.height <= 780);
        }
    }
}
