using UnityEngine.UIElements;

namespace NetLab.Controllers
{
    public sealed class ToastController
    {
        private readonly VisualElement _toast;
        private readonly Label _message;
        private IVisualElementScheduledItem _scheduledHide;

        public ToastController(VisualElement root)
        {
            _toast = root.Q<VisualElement>("toast");
            _message = root.Q<Label>("toast-message");
            _toast.style.display = DisplayStyle.None;
        }

        public void Show(string message)
        {
            _scheduledHide?.Pause();
            _message.text = message;
            _toast.style.display = DisplayStyle.Flex;
            _toast.RemoveFromClassList("visible");
            _toast.schedule.Execute(() => _toast.AddToClassList("visible"));
            _scheduledHide = _toast.schedule.Execute(() =>
            {
                _toast.RemoveFromClassList("visible");
                _toast.style.display = DisplayStyle.None;
            }).StartingIn(2400);
        }
    }
}
