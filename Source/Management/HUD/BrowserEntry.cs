using Godot;

namespace Aquamarine.Source.Management.HUD;

public partial class BrowserEntry : Control
{
    [Export] public Button DetailsButton;
    [Export] public Button QuickButton;

    [Export] private RichTextLabelAutoSize Label;
    [Export] private RichTextLabelAutoSize DetailText;

    public void Setup(string label, string detailText)
    {
        Label.Text = label;
        DetailText.Text = detailText;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }
}
