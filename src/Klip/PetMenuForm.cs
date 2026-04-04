using System;
using System.Drawing;
using System.Windows.Forms;

namespace Klip;

public sealed class PetMenuForm : Form
{
    private const int DefaultWindowWidth = 340;
    private const int DefaultWindowHeight = 240;

    private const int MinContentWidth = 280;
    private const int MinContentHeight = 180;
    private const int MaxContentWidth = 420;
    private const int MaxContentHeight = 320;

    private const int OuterPadding = 10;
    private const int ToolbarHeight = 30;
    private const int ContentTop = OuterPadding + ToolbarHeight + 8;

    private readonly Panel rootPanel = new();
    private readonly Label typeBadge = new();
    private readonly Button clearButton = new();
    private readonly Button cancelButton = new();
    private readonly Button saveButton = new();
    private readonly TextBox textEditor = new();
    private readonly PictureBox imageBox = new();

    private bool allowClose;
    private bool isDirty;
    private bool isApplyingContent;

    private ClipboardViewKind currentKind;
    private string currentTypeTag = "TXT";
    private string currentText = string.Empty;
    private Image? currentImage;

    public PetMenuForm()
    {
        ConfigureWindow();
        InitializeRoot();
        InitializeToolbar();
        InitializeTextEditor();
        InitializeImageBox();

        Controls.Add(rootPanel);

        LoadClipboardSnapshot();
        ApplyCurrentContent();
        UpdateDirtyState(false);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        DisposeCurrentImage();
        base.OnFormClosed(e);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!allowClose && isDirty)
        {
            e.Cancel = true;
            FlashSaveButton();
            return;
        }

        base.OnFormClosing(e);
    }

    private void ConfigureWindow()
    {
        StartPosition = FormStartPosition.Manual;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        KeyPreview = true;
        BackColor = Color.FromArgb(10, 12, 16);
        ClientSize = new Size(DefaultWindowWidth, DefaultWindowHeight);

        KeyDown += OnFormKeyDown;
    }

    private void InitializeRoot()
    {
        rootPanel.Dock = DockStyle.Fill;
        rootPanel.BackColor = Color.FromArgb(18, 21, 27);
        rootPanel.Padding = new Padding(OuterPadding);
    }

    private void InitializeToolbar()
    {
        ConfigureToolbarButton(clearButton, "Clear", new Point(OuterPadding, OuterPadding));
        ConfigureToolbarButton(cancelButton, "Cancel", new Point(OuterPadding + 72, OuterPadding));

        saveButton.Size = new Size(64, 28);
        saveButton.Location = new Point(OuterPadding + 144, OuterPadding);
        saveButton.FlatStyle = FlatStyle.Flat;
        saveButton.FlatAppearance.BorderSize = 1;
        saveButton.FlatAppearance.BorderColor = Color.FromArgb(72, 78, 90);
        saveButton.BackColor = Color.FromArgb(52, 58, 70);
        saveButton.ForeColor = Color.FromArgb(235, 239, 245);
        saveButton.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        saveButton.TabStop = false;
        saveButton.Text = "Save";
        saveButton.Click += (_, _) => SaveAndClose();

        clearButton.Click += (_, _) => ClearContent();
        cancelButton.Click += (_, _) => CancelAndClose();

        typeBadge.AutoSize = false;
        typeBadge.Size = new Size(76, 24);
        typeBadge.TextAlign = ContentAlignment.MiddleCenter;
        typeBadge.Location = new Point(ClientSize.Width - OuterPadding - 76, OuterPadding + 2);
        typeBadge.BackColor = Color.FromArgb(44, 49, 58);
        typeBadge.ForeColor = Color.FromArgb(235, 239, 245);
        typeBadge.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);

        rootPanel.Controls.Add(clearButton);
        rootPanel.Controls.Add(cancelButton);
        rootPanel.Controls.Add(saveButton);
        rootPanel.Controls.Add(typeBadge);
    }

    private void ConfigureToolbarButton(Button button, string text, Point location)
    {
        button.Size = new Size(64, 28);
        button.Location = location;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = Color.FromArgb(72, 78, 90);
        button.BackColor = Color.FromArgb(34, 38, 46);
        button.ForeColor = Color.FromArgb(228, 232, 238);
        button.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        button.TabStop = false;
        button.Text = text;
    }

    private void InitializeTextEditor()
    {
        textEditor.Location = new Point(OuterPadding, ContentTop);
        textEditor.Size = GetContentAreaSize();
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

        rootPanel.Controls.Add(textEditor);
    }

    private void InitializeImageBox()
    {
        imageBox.Location = new Point(OuterPadding, ContentTop);
        imageBox.Size = GetContentAreaSize();
        imageBox.SizeMode = PictureBoxSizeMode.Zoom;
        imageBox.BackColor = Color.FromArgb(24, 27, 34);
        imageBox.Visible = false;
        imageBox.Cursor = Cursors.Hand;
        imageBox.Click += OnImageClicked;

        rootPanel.Controls.Add(imageBox);
    }

    private void OnFormKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            if (isDirty)
            {
                FlashSaveButton();
            }
            else
            {
                CancelAndClose();
            }

            e.Handled = true;
            return;
        }

        if (e.Control && e.KeyCode == Keys.S)
        {
            SaveAndClose();
            e.Handled = true;
        }
    }

    private void LoadClipboardSnapshot()
    {
        Utils.ExecuteClipboardAction(() =>
        {
            IDataObject? data = Clipboard.GetDataObject();
            string[] formats = data?.GetFormats() ?? Array.Empty<string>();

            currentKind = ClipboardViewKind.Text;
            currentTypeTag = "TXT";
            currentText = string.Empty;

            DisposeCurrentImage();

            if (data is null)
            {
                return;
            }

            if (data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = data.GetData(DataFormats.FileDrop) as string[] ?? Array.Empty<string>();

                currentTypeTag = "FILE";
                currentText = files.Length > 0
                    ? string.Join(Environment.NewLine, files)
                    : "Clipboard contains files.";
                return;
            }

            if (data.GetDataPresent(DataFormats.Html))
            {
                currentTypeTag = "HTML";
                currentText = Clipboard.ContainsText()
                    ? Clipboard.GetText()
                    : "Clipboard contains HTML content.";
                return;
            }

            if (data.GetDataPresent(DataFormats.Rtf))
            {
                currentTypeTag = "RTF";
                currentText = Clipboard.ContainsText()
                    ? Clipboard.GetText()
                    : "Clipboard contains rich text.";
                return;
            }

            if (data.GetDataPresent("PNG") || data.GetDataPresent(DataFormats.Bitmap) || Clipboard.ContainsImage())
            {
                Image? image = Clipboard.GetImage();

                if (image is not null)
                {
                    currentKind = ClipboardViewKind.Image;
                    currentTypeTag = "IMG";
                    currentImage = (Image)image.Clone();
                    return;
                }
            }

            if (Clipboard.ContainsText())
            {
                currentTypeTag = "TXT";
                currentText = Clipboard.GetText();
                return;
            }

            string fallbackFormat = GetPreferredUnknownFormat(formats);

            if (!string.IsNullOrEmpty(fallbackFormat))
            {
                currentTypeTag = FormatTag(fallbackFormat);
                currentText = $"Clipboard contains data in format: {fallbackFormat}";
                return;
            }

            currentTypeTag = "EMPTY";
            currentText = string.Empty;
        });
    }

    private void ApplyCurrentContent()
    {
        switch (currentKind)
        {
            case ClipboardViewKind.Image:
                ApplyImageContent();
                break;

            default:
                ApplyTextContent();
                break;
        }
    }

    private void ApplyTextContent()
    {
        textEditor.Visible = true;
        textEditor.ReadOnly = false;
        imageBox.Visible = false;
        imageBox.Image = null;

        isApplyingContent = true;

        try
        {
            if (!string.Equals(textEditor.Text, currentText, StringComparison.Ordinal))
            {
                textEditor.Text = currentText;
            }
        }
        finally
        {
            isApplyingContent = false;
        }

        typeBadge.Text = currentTypeTag;
        UpdateBadgeColor(currentTypeTag);

        ClientSize = new Size(DefaultWindowWidth, DefaultWindowHeight);
        UpdateLayoutForCurrentSize();
    }

    private void ApplyImageContent()
    {
        textEditor.Visible = false;
        imageBox.Visible = true;
        imageBox.Image = currentImage;

        typeBadge.Text = currentTypeTag;
        UpdateBadgeColor(currentTypeTag);

        ResizeForImage(currentImage);
        UpdateLayoutForCurrentSize();
    }

    private void OnTextEditorChanged(object? sender, EventArgs e)
    {
        if (currentKind != ClipboardViewKind.Text || isApplyingContent)
        {
            return;
        }

        currentText = textEditor.Text;

        if (currentTypeTag != "TXT")
        {
            currentTypeTag = "TXT";
            typeBadge.Text = currentTypeTag;
            UpdateBadgeColor(currentTypeTag);
        }

        UpdateDirtyState(true);
    }

    private void ClearContent()
    {
        DisposeCurrentImage();

        currentKind = ClipboardViewKind.Text;
        currentTypeTag = "EMPTY";
        currentText = string.Empty;

        ApplyCurrentContent();
        textEditor.Focus();
        UpdateDirtyState(true);
    }

    private void SaveAndClose()
    {
        try
        {
            WriteCurrentContentToClipboard();
            allowClose = true;
            Close();
        }
        catch
        {
            FlashSaveButton();
        }
    }

    private void CancelAndClose()
    {
        allowClose = true;
        Close();
    }

    private void WriteCurrentContentToClipboard()
    {
        switch (currentKind)
        {
            case ClipboardViewKind.Image:
                Utils.WriteClipboardImage(currentImage);
                break;

            default:
                Utils.WriteClipboardText(currentText);
                break;
        }
    }

    private void UpdateDirtyState(bool dirty)
    {
        isDirty = dirty;

        saveButton.Enabled = dirty;
        saveButton.BackColor = dirty
            ? Color.FromArgb(86, 114, 176)
            : Color.FromArgb(52, 58, 70);

        saveButton.ForeColor = dirty
            ? Color.FromArgb(245, 248, 255)
            : Color.FromArgb(180, 186, 196);
    }

    private void FlashSaveButton()
    {
        saveButton.Focus();
    }

    private void ResizeForImage(Image? image)
    {
        if (image is null)
        {
            ClientSize = new Size(DefaultWindowWidth, DefaultWindowHeight);
            return;
        }

        int contentWidth = Math.Max(1, image.Width);
        int contentHeight = Math.Max(1, image.Height);

        float scale = Math.Min(
            (float)MaxContentWidth / contentWidth,
            (float)MaxContentHeight / contentHeight);

        scale = Math.Min(scale, 1f);

        int fittedWidth = Math.Max(MinContentWidth, (int)Math.Round(contentWidth * scale));
        int fittedHeight = Math.Max(MinContentHeight, (int)Math.Round(contentHeight * scale));

        ClientSize = new Size(
            fittedWidth + OuterPadding * 2,
            fittedHeight + ContentTop + OuterPadding);
    }

    private void UpdateLayoutForCurrentSize()
    {
        clearButton.Location = new Point(OuterPadding, OuterPadding);
        cancelButton.Location = new Point(OuterPadding + 72, OuterPadding);
        saveButton.Location = new Point(OuterPadding + 144, OuterPadding);
        typeBadge.Location = new Point(ClientSize.Width - OuterPadding - typeBadge.Width, OuterPadding + 2);

        Size contentArea = GetContentAreaSize();

        textEditor.Location = new Point(OuterPadding, ContentTop);
        textEditor.Size = contentArea;

        imageBox.Location = new Point(OuterPadding, ContentTop);
        imageBox.Size = contentArea;
    }

    private Size GetContentAreaSize()
    {
        return new Size(
            Math.Max(1, ClientSize.Width - OuterPadding * 2),
            Math.Max(1, ClientSize.Height - ContentTop - OuterPadding));
    }

    private void OnImageClicked(object? sender, EventArgs e)
    {
        if (currentImage is null)
        {
            return;
        }

        using FullscreenImageForm preview = new(currentImage);
        preview.ShowDialog(this);
    }

    private void DisposeCurrentImage()
    {
        imageBox.Image = null;

        if (currentImage is null)
        {
            return;
        }

        currentImage.Dispose();
        currentImage = null;
    }

    private void UpdateBadgeColor(string tag)
    {
        switch (tag)
        {
            case "HTML":
                typeBadge.BackColor = Color.FromArgb(91, 62, 34);
                break;

            case "RTF":
                typeBadge.BackColor = Color.FromArgb(70, 52, 92);
                break;

            case "FILE":
                typeBadge.BackColor = Color.FromArgb(52, 84, 64);
                break;

            case "PNG":
            case "IMG":
                typeBadge.BackColor = Color.FromArgb(38, 67, 92);
                break;

            case "EMPTY":
                typeBadge.BackColor = Color.FromArgb(62, 62, 62);
                break;

            default:
                typeBadge.BackColor = Color.FromArgb(44, 49, 58);
                break;
        }
    }

    private static string GetPreferredUnknownFormat(string[] formats)
    {
        foreach (string format in formats)
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                continue;
            }

            if (format == DataFormats.Text)
            {
                continue;
            }

            if (format == DataFormats.UnicodeText)
            {
                continue;
            }

            if (format == DataFormats.StringFormat)
            {
                continue;
            }

            if (format == DataFormats.Bitmap)
            {
                continue;
            }

            if (format == DataFormats.Html)
            {
                continue;
            }

            if (format == DataFormats.Rtf)
            {
                continue;
            }

            if (format == DataFormats.FileDrop)
            {
                continue;
            }

            if (format == "System.String")
            {
                continue;
            }

            if (format == "PNG")
            {
                continue;
            }

            return format;
        }

        return string.Empty;
    }

    private static string FormatTag(string format)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            return "DATA";
        }

        string tag = format.Trim().ToUpperInvariant();

        int lastDot = tag.LastIndexOf('.');
        if (lastDot >= 0 && lastDot < tag.Length - 1)
        {
            tag = tag[(lastDot + 1)..];
        }

        tag = tag.Replace("FORMAT", string.Empty);
        tag = tag.Replace("_", string.Empty);
        tag = tag.Replace("-", string.Empty);
        tag = tag.Replace(" ", string.Empty);

        if (tag.Length == 0)
        {
            return "DATA";
        }

        if (tag.Length > 6)
        {
            tag = tag[..6];
        }

        return tag;
    }

    private enum ClipboardViewKind
    {
        Text,
        Image
    }
}
