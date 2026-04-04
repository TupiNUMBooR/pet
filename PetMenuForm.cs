using System.Drawing;
using System.Windows.Forms;

namespace Pet;

public sealed class PetMenuForm : Form
{
    public PetMenuForm()
    {
        Text = "Pet";
        StartPosition = FormStartPosition.Manual;
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        ShowInTaskbar = false;
        MaximizeBox = false;
        MinimizeBox = false;
        TopMost = true;

        ClientSize = new Size(220, 120);
    }
}
