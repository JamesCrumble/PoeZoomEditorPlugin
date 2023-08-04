using ExileCore.Shared.Nodes;
using ExileCore.Shared.Interfaces;

namespace ZoomEditor
{
    public class ZoomEditorSettings : ISettings
    {
        public ZoomEditorSettings()
        {
            Enable = new ToggleNode(true);
            ZoomValue = new TextNode("-1.0");
            DebugOutput = new ToggleNode(false);
            Reload = new ButtonNode();
        }
        public ToggleNode Enable { get; set; }
        public TextNode ZoomValue { get; set; }
        public ToggleNode DebugOutput { get; set; }
        public ButtonNode Reload { get; set; }

    }
}
