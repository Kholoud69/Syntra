using System.Collections.Generic;
using NetLab.Models;
using UnityEngine.UIElements;

namespace NetLab.Controllers
{
    public sealed class AiChatController
    {
        private readonly VisualElement _root;
        private readonly ScrollView _history;
        private readonly TextField _input;
        private readonly ToastController _toast;
        private readonly List<ChatMessageModel> _messages = new();

        public AiChatController(VisualElement root, ToastController toast)
        {
            _root = root;
            _toast = toast;
            _history = root.Q<ScrollView>("chat-history");
            _input = root.Q<TextField>("chat-input");

            root.Q<Button>("ai-settings").clicked += () => toast.Show("AI preferences opened");
            root.Q<Button>("diagnose-ping").clicked += () => AddMessage(new ChatMessageModel(
                ChatRole.Ai,
                "Ping diagnostic started. PC-01 reaches its gateway, but Router-01 has no matching route for 10.0.2.0/24."));
            root.Q<Button>("quiz-me").clicked += () => AddMessage(new ChatMessageModel(
                ChatRole.Ai,
                "Practice question: Which command would verify that Router-01 installed the new static route?"));
            root.Q<Button>("send-message").clicked += Send;
            _input.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == UnityEngine.KeyCode.Return &&
                    !evt.shiftKey)
                {
                    Send();
                    evt.StopPropagation();
                }
            });

            AddMessage(new ChatMessageModel(
                ChatRole.Ai,
                "The ping from PC-01 to Server-01 is failing because Router-01 has no route to the 10.0.2.0/24 network."));
            AddMessage(new ChatMessageModel(
                ChatRole.Ai,
                "Add a static route through 10.0.1.2, then verify the return path on Router-02."));
        }

        private void Send()
        {
            var text = _input.value?.Trim();
            if (string.IsNullOrEmpty(text))
                return;
            AddMessage(new ChatMessageModel(ChatRole.User, text));
            _input.value = string.Empty;
        }

        private void AddMessage(ChatMessageModel message)
        {
            _messages.Add(message);
            var row = new VisualElement();
            row.AddToClassList("message");
            row.AddToClassList(message.Role == ChatRole.Ai ? "ai" : "user");
            if (message.Role == ChatRole.Ai)
            {
                var badge = new Label("AI");
                badge.AddToClassList("message-avatar");
                row.Add(badge);
            }
            var text = new Label(message.Text);
            text.AddToClassList("message-text");
            row.Add(text);

            var guide = _history.Q("guide-card");
            var container = _history.contentContainer;
            container.Insert(container.IndexOf(guide), row);
            _history.schedule.Execute(() => _history.ScrollTo(guide));
        }
    }
}
