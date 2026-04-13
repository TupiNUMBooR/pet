using System.Drawing;
using System.Windows.Forms;

namespace Klip;

public sealed partial class PetMenuForm
{
    private Panel rootPanel = null!;
    private Label typeBadge = null!;
    private Button clearButton = null!;
    private Button cancelButton = null!;
    private Button saveButton = null!;
    private TextBox textEditor = null!;
    private PictureBox imageBox = null!;

    private void InitializeComponent()
    {
        rootPanel = new Panel();
        typeBadge = new Label();
        clearButton = new Button();
        cancelButton = new Button();
        saveButton = new Button();
        textEditor = new TextBox();
        imageBox = new PictureBox();

        SuspendLayout();
        rootPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)imageBox).BeginInit();

        StartPosition = FormStartPosition.Manual;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        KeyPreview = true;
        BackColor = Color.FromArgb(10, 12, 16);
        ClientSize = new Size(DefaultWindowWidth, DefaultWindowHeight);
        KeyDown += OnFormKeyDown;

        rootPanel.Dock = DockStyle.Fill;
        rootPanel.BackColor = Color.FromArgb(18, 21, 27);
        rootPanel.Padding = new Padding(OuterPadding);

        ConfigureToolbarButton(clearButton, "Clear", new Point(OuterPadding, OuterPadding), false);
        ConfigureToolbarButton(cancelButton, "Cancel", new Point(OuterPadding + 72, OuterPadding), false);
        ConfigureToolbarButton(saveButton, "Save", new Point(OuterPadding + 144, OuterPadding), true);

        clearButton.Click += (_, _) => ClearContent();
        cancelButton.Click += (_, _) => CancelAndClose();
        saveButton.Click += (_, _) => SaveAndClose();

        typeBadge.AutoSize = false;
        typeBadge.Size = new Size(76, 24);
        typeBadge.Location = new Point(ClientSize.Width - OuterPadding - typeBadge.Width, OuterPadding + 2);
        typeBadge.TextAlign = ContentAlignment.MiddleCenter;
        typeBadge.BackColor = Color.FromArgb(44, 49, 58);
        typeBadge.ForeColor = Color.FromArgb(235, 239, 245);
        typeBadge.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);

        textEditor.Location = new Point(OuterPadding, ContentTop);
        textEditor.Size = new Size(
            ClientSize.Width - OuterPadding * 2,
            ClientSize.Height - ContentTop - OuterPadding);
        textEditor.Multiline = true;
        textEditor.AcceptsReturn = true;
        textEditor.AcceptsTab = false;
        textEditor.WordWrap = true;
        textEditor.ScrollBars = ScrollBars.Vertical;
        textEditor.BorderStyle = BorderStyle.None;
        textEditor.BackColor = Color.FromArgb(24, 27, 34);
        textEditor.ForeColor = Color.FromArgb(226, 230, 236);
        textEditor.Font = new Font("Consolas", 10f);
        textEditor.TextChanged += OnTextEditorChanged;

        imageBox.Location = new Point(OuterPadding, ContentTop);
        imageBox.Size = textEditor.Size;
        imageBox.SizeMode = PictureBoxSizeMode.Zoom;
        imageBox.BackColor = Color.FromArgb(24, 27, 34);
        imageBox.Visible = false;
        imageBox.Cursor = Cursors.Hand;
        imageBox.Click += OnImageClicked;

        rootPanel.Controls.Add(clearButton);
        rootPanel.Controls.Add(cancelButton);
        rootPanel.Controls.Add(saveButton);
        rootPanel.Controls.Add(typeBadge);
        rootPanel.Controls.Add(textEditor);
        rootPanel.Controls.Add(imageBox);

        Controls.Add(rootPanel);

        rootPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)imageBox).EndInit();
        ResumeLayout(false);
    }

    private void ConfigureToolbarButton(Button button, string text, Point location, bool primary)
    {
        button.Size = new Size(64, 28);
        button.Location = location;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = Color.FromArgb(72, 78, 90);
        button.BackColor = primary
            ? Color.FromArgb(52, 58, 70)
            : Color.FromArgb(34, 38, 46);
        button.ForeColor = primary
            ? Color.FromArgb(235, 239, 245)
            : Color.FromArgb(228, 232, 238);
        button.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        button.TabStop = false;
        button.Text = text;
        button.UseVisualStyleBackColor = false;
    }
}
