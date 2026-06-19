using UnityEngine;
using UnityEngine.UIElements;

namespace MovementMD.UI
{
    // Centralized colors/sizes + element factories so the menus are easy to retune in one place.
    // Inline styles (not USS) keep the shell runnable with zero asset wiring; migrate to
    // UXML/USS via UI Builder later if desired.
    internal static class UITheme
    {
        public static readonly Color Bg = new(0.07f, 0.08f, 0.10f, 0.96f);
        public static readonly Color Panel = new(0.11f, 0.12f, 0.15f, 0.98f);
        public static readonly Color Accent = new(0.36f, 0.78f, 1.00f, 1f);
        public static readonly Color Text = Color.white;
        public static readonly Color Muted = new(0.60f, 0.62f, 0.68f, 1f);

        public const int TitleSize = 46;
        public const int BodySize = 16;
        public const int ButtonHeight = 46;

        public static Button MakeButton(string text, string name = null)
        {
            var b = new Button { text = text };
            if (!string.IsNullOrEmpty(name)) b.name = name;
            b.style.height = ButtonHeight;
            b.style.fontSize = BodySize;
            b.style.color = Text;
            b.style.backgroundColor = Panel;
            b.style.borderBottomColor = Accent;
            b.style.borderBottomWidth = 2;
            b.style.marginTop = 4;
            b.style.marginBottom = 4;
            b.style.borderTopLeftRadius = 3;
            b.style.borderTopRightRadius = 3;
            b.style.borderBottomLeftRadius = 3;
            b.style.borderBottomRightRadius = 3;
            return b;
        }

        public static Label MakeLabel(string text, int size = BodySize, Color color = default, string name = null)
        {
            var l = new Label(text);
            if (!string.IsNullOrEmpty(name)) l.name = name;
            l.style.fontSize = size;
            l.style.color = color == default ? Text : color;
            return l;
        }

        public static VisualElement Spacer(float height)
        {
            var s = new VisualElement { style = { height = height } };
            return s;
        }
    }
}
