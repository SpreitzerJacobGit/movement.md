using UnityEngine.UIElements;

namespace MovementMD.Dev
{
    /// <summary>
    /// One developer-menu tool. <see cref="CreateControl"/> returns the tool's UI (built in code),
    /// embedded into the dev panel by <c>DevMenuController</c>. Tools self-register into
    /// <see cref="DevToolRegistry"/>.
    /// </summary>
    public interface IDevTool
    {
        string DisplayName { get; }
        VisualElement CreateControl();
    }
}
