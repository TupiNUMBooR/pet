using System;
using System.Drawing;
using System.Windows.Forms;

namespace Klip;

public sealed partial class PetMenuForm : Form
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

    private bool allowClose;
    private bool isDirty;
    private bool isApplyingContent;

    private ClipboardViewKind currentKind;
    private string currentTypeTag = "TXT";
    private string currentText = string.Empty;
    private Image? currentImage;

    public PetMenuForm()
    {
        InitializeComponent();

        LoadClipboardSnapshot();
        ApplyCurrentContent();
        UpdateDirtyState(false);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!allowClose && isDirty)
        {
            e.Cancel = true;
            saveButton.Focus();
            return;
        }

        base.OnFormClosing(e);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        imageBox.Image = null;
        currentImage?.Dispose();
        currentImage = null;

        base.OnFormClosed(e);
    }

    private void OnFormKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            if (isDirty)
            {
                saveButton.Focus();
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

    private void OnTextEditorChanged(object? sender, EventArgs e)
    {
        if (currentKind != ClipboardViewKind.Text || isApplyingContent)
        {
            return;
        }

        currentText = textEditor.Text;

        if (currentTypeTag != "TXT")
        {
            SetTypeTag("TXT");
        }

        UpdateDirtyState(true);
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

    private void LoadClipboardSnapshot()
    {
        Utils.ExecuteClipboardAction(() =>
        {
            IDataObject? data = Clipboard.GetDataObject();
            string[] formats = data?.GetFormats() ?? Array.Empty<string>();

            currentKind = ClipboardViewKind.Text;
            currentText = string.Empty;

            imageBox.Image = null;
            currentImage?.Dispose();
            currentImage = null;

            if (data is null)
            {
                SetTypeTag("TXT");
                return;
            }

            if (data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = data.GetData(DataFormats.FileDrop) as string[] ?? Array.Empty<string>();
                currentText = files.Length > 0
                    ? string.Join(Environment.NewLine, files)
                    : "Clipboard contains files.";
                SetTypeTag("FILE");
                return;
            }

            if (data.GetDataPresent(DataFormats.Html))
            {
                currentText = Clipboard.ContainsText()
                    ? Clipboard.GetText()
                    : "Clipboard contains HTML content.";
                SetTypeTag("HTML");
                return;
            }

            if (data.GetDataPresent(DataFormats.Rtf))
            {
                currentText = Clipboard.ContainsText()
                    ? Clipboard.GetText()
                    : "Clipboard contains rich text.";
                SetTypeTag("RTF");
                return;
            }

            if (data.GetDataPresent("PNG") || data.GetDataPresent(DataFormats.Bitmap) || Clipboard.ContainsImage())
            {
                Image? image = Clipboard.GetImage();

                if (image is not null)
                {
                    currentKind = ClipboardViewKind.Image;
                    currentImage = (Image)image.Clone();
                    SetTypeTag("IMG");
                    return;
                }
            }

            if (Clipboard.ContainsText())
            {
                currentText = Clipboard.GetText();
                SetTypeTag("TXT");
                return;
            }

            string fallbackFormat = GetPreferredUnknownFormat(formats);

            if (!string.IsNullOrEmpty(fallbackFormat))
            {
                currentText = $"Clipboard contains data in format: {fallbackFormat}";
                SetTypeTag(FormatTag(fallbackFormat));
                return;
            }

            currentText = string.Empty;
            SetTypeTag("EMPTY");
        });
    }

    private void ApplyCurrentContent()
    {
        if (currentKind == ClipboardViewKind.Image)
        {
            textEditor.Visible = false;
            imageBox.Visible = true;
            imageBox.Image = currentImage;

            ResizeForImage(currentImage);
            UpdateLayoutForCurrentSize();
            return;
        }

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

        ClientSize = new Size(DefaultWindowWidth, DefaultWindowHeight);
        UpdateLayoutForCurrentSize();
    }

    private void ClearContent()
    {
        imageBox.Image = null;
        currentImage?.Dispose();
        currentImage = null;

        currentKind = ClipboardViewKind.Text;
        currentText = string.Empty;
        SetTypeTag("EMPTY");

        ApplyCurrentContent();
        textEditor.Focus();
        UpdateDirtyState(true);
    }

    private void SaveAndClose()
    {
        try
        {
            if (currentKind == ClipboardViewKind.Image)
            {
                Utils.WriteClipboardImage(currentImage);
            }
            else
            {
                Utils.WriteClipboardText(currentText);
            }

            allowClose = true;
            Close();
        }
        catch
        {
            saveButton.Focus();
        }
    }

    private void CancelAndClose()
    {
        allowClose = true;
        Close();
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

    private void SetTypeTag(string tag)
    {
        currentTypeTag = tag;
        typeBadge.Text = tag;

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

        Size contentArea = new(
            Math.Max(1, ClientSize.Width - OuterPadding * 2),
            Math.Max(1, ClientSize.Height - ContentTop - OuterPadding));

        Point contentLocation = new(OuterPadding, ContentTop);

        textEditor.Location = contentLocation;
        textEditor.Size = contentArea;

        imageBox.Location = contentLocation;
        imageBox.Size = contentArea;
    }

    private static string GetPreferredUnknownFormat(string[] formats)
    {
        foreach (string format in formats)
        {
            if (string.IsNullOrWhiteSpace(format)
                || format == DataFormats.Text
                || format == DataFormats.UnicodeText
                || format == DataFormats.StringFormat
                || format == DataFormats.Bitmap
                || format == DataFormats.Html
                || format == DataFormats.Rtf
                || format == DataFormats.FileDrop
                || format == "System.String"
                || format == "PNG")
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

        tag = tag.Replace("FORMAT", string.Empty)
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .Replace(" ", string.Empty);

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
