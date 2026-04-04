using System;
using System.Drawing;
using System.Windows.Forms;

namespace Pet;

public sealed class PetMenuForm : Form
{
    private const string WindowTitle = "Clip's Memory";

    private const int WindowWidth = 280;
    private const int WindowHeight = 180;

    private const int OuterPadding = 12;
    private const int EditorHeight = 110;
    private const int BorderThickness = 1;

    private const int ButtonWidth = 80;
    private const int ButtonHeight = 36;
    private const int ButtonsTop = 132;

    private const string CopyButtonText = "Copy";
    private const string PasteButtonText = "Paste";

    // private const string CopyButtonText = "C";
    // private const string PasteButtonText = "V";
    // private const string CopyButtonText = "⧉";
    // private const string PasteButtonText = "⇩";
    // private const string CopyButtonText = "📋";
    // private const string PasteButtonText = "📥";

    private const string CopyButtonTooltip = "Copy";
    private const string PasteButtonTooltip = "Paste";

    private readonly Panel editorHost = new();
    private readonly TextBox editor = new();
    private readonly Button copyButton = new();
    private readonly Button pasteButton = new();
    private readonly ToolTip toolTip = new();

    public string EditorText
    {
        get => editor.Text;
        set => editor.Text = value;
    }

    public PetMenuForm()
    {
        Text = WindowTitle;
        StartPosition = FormStartPosition.Manual;
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        ShowInTaskbar = false;
        MaximizeBox = false;
        MinimizeBox = false;
        TopMost = true;

        BackColor = Color.FromArgb(16, 18, 22);
        ClientSize = new Size(WindowWidth, WindowHeight);

        InitializeEditor();
        InitializeButtons();

        Controls.Add(editorHost);
        Controls.Add(copyButton);
        Controls.Add(pasteButton);
    }

    private void InitializeEditor()
    {
        editorHost.Location = new Point(OuterPadding, OuterPadding);
        editorHost.Size = new Size(
            ClientSize.Width - OuterPadding * 2,
            EditorHeight);

        editorHost.BackColor = Color.FromArgb(56, 60, 68);

        editor.Location = new Point(BorderThickness, BorderThickness);
        editor.Size = new Size(
            editorHost.Width - BorderThickness * 2,
            editorHost.Height - BorderThickness * 2);

        editor.BorderStyle = BorderStyle.None;
        editor.Multiline = true;
        editor.WordWrap = true;
        editor.ScrollBars = ScrollBars.Vertical;
        editor.AcceptsReturn = true;
        editor.AcceptsTab = false;

        editor.Font = new Font("Segoe UI", 9f);
        editor.BackColor = Color.FromArgb(28, 31, 36);
        editor.ForeColor = Color.FromArgb(222, 226, 232);

        editorHost.Controls.Add(editor);
    }

    private void InitializeButtons()
    {
        ConfigureButton(copyButton, CopyButtonText, CopyButtonTooltip);
        ConfigureButton(pasteButton, PasteButtonText, PasteButtonTooltip);

        copyButton.Location = new Point(OuterPadding, ButtonsTop);

        pasteButton.Location = new Point(
            ClientSize.Width - OuterPadding - ButtonWidth,
            ButtonsTop);

        copyButton.Click += (_, _) => CopyEditorText();
        pasteButton.Click += (_, _) => PasteClipboardText();
    }

    private void ConfigureButton(Button button, string text, string tooltipText)
    {
        button.Size = new Size(ButtonWidth, ButtonHeight);

        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = Color.FromArgb(70, 76, 86);

        button.BackColor = Color.FromArgb(34, 37, 43);
        button.ForeColor = Color.FromArgb(228, 232, 238);

        button.Font = new Font("Segoe UI", 9f, FontStyle.Bold);

        button.Text = text;
        button.TabStop = false;

        toolTip.SetToolTip(button, tooltipText);

        button.MouseEnter += (_, _) =>
            button.BackColor = Color.FromArgb(44, 48, 56);

        button.MouseLeave += (_, _) =>
            button.BackColor = Color.FromArgb(34, 37, 43);
    }

    private void CopyEditorText()
    {
        try
        {
            Clipboard.SetText(editor.Text);
        }
        catch
        {
        }
    }

    private void PasteClipboardText()
    {
        try
        {
            if (!Clipboard.ContainsText())
                return;

            editor.Text = Clipboard.GetText();
            editor.Focus();
        }
        catch
        {
        }
    }
}
