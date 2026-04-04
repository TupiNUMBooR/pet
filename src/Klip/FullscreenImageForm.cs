using System;
using System.Drawing;
using System.Windows.Forms;

namespace Klip;

public sealed class FullscreenImageForm : Form
{
    private readonly PictureBox previewBox = new();

    public FullscreenImageForm(Image image)
    {
        StartPosition = FormStartPosition.Manual;
        FormBorderStyle = FormBorderStyle.None;
        Bounds = Screen.FromPoint(Cursor.Position).Bounds;
        TopMost = true;
        ShowInTaskbar = false;
        BackColor = Color.Black;
        KeyPreview = true;

        previewBox.Dock = DockStyle.Fill;
        previewBox.BackColor = Color.Black;
        previewBox.SizeMode = PictureBoxSizeMode.Zoom;
        previewBox.Image = (Image)image.Clone();

        Controls.Add(previewBox);

        KeyDown += (_, _) => Close();
        MouseDown += (_, _) => Close();
        previewBox.MouseDown += (_, _) => Close();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        if (previewBox.Image is not null)
        {
            previewBox.Image.Dispose();
            previewBox.Image = null;
        }

        base.OnFormClosed(e);
    }
}
