using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace VeilView;

internal sealed class GestureSettingsDialog : Form
{
    private readonly CheckBox _enabledCheckBox = new();
    private readonly Dictionary<string, ComboBox> _actionBoxes = new(StringComparer.OrdinalIgnoreCase);

    public GestureSettingsDialog(AppSettings settings)
    {
        Text = "마우스 제스처 설정";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(450, 392);
        Padding = new Padding(12);
        ShowInTaskbar = false;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 12));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _enabledCheckBox.Text = "마우스 제스처 사용";
        _enabledCheckBox.AutoSize = true;
        _enabledCheckBox.Checked = settings.MouseGesturesEnabled;
        root.Controls.Add(_enabledCheckBox, 0, 0);

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = GesturePatterns.All.Length + 1,
            AutoScroll = false
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 54));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));

        AddHeader(grid, "제스처", 0, 0);
        AddHeader(grid, "인식 형태", 1, 0);
        AddHeader(grid, "실행 동작", 2, 0);

        var normalized = settings.GetNormalizedGestureActions();
        for (var i = 0; i < GesturePatterns.All.Length; i++)
        {
            var pattern = GesturePatterns.All[i];
            var row = i + 1;

            grid.Controls.Add(new Label
            {
                Text = pattern.Display,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font(Font.FontFamily, 14, FontStyle.Bold)
            }, 0, row);

            grid.Controls.Add(new Label
            {
                Text = pattern.Description,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            }, 1, row);

            var combo = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            foreach (var action in GestureActions.All)
            {
                combo.Items.Add(new ComboItem(action.Key, action.Display));
            }

            var selectedAction = normalized.TryGetValue(pattern.Key, out var value) ? value : GestureActions.None;
            combo.SelectedIndex = Math.Max(0, GestureActions.All.ToList().FindIndex(item => item.Key.Equals(selectedAction, StringComparison.OrdinalIgnoreCase)));
            _actionBoxes[pattern.Key] = combo;
            grid.Controls.Add(combo, 2, row);
        }

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true
        };

        var okButton = new Button { Text = "저장", Width = 82, DialogResult = DialogResult.OK };
        var cancelButton = new Button { Text = "취소", Width = 82, DialogResult = DialogResult.Cancel };
        var defaultsButton = new Button { Text = "기본값", Width = 82 };
        defaultsButton.Click += (_, _) => ApplyDefaults();

        buttonPanel.Controls.Add(okButton);
        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(defaultsButton);

        root.Controls.Add(new Panel(), 0, 1);
        root.Controls.Add(grid, 0, 2);
        root.Controls.Add(buttonPanel, 0, 3);

        Controls.Add(root);
        AcceptButton = okButton;
        CancelButton = cancelButton;
    }

    public bool GesturesEnabled => _enabledCheckBox.Checked;

    public Dictionary<string, string> SelectedActions
        => _actionBoxes.ToDictionary(
            item => item.Key,
            item => ((ComboItem)item.Value.SelectedItem!).Value,
            StringComparer.OrdinalIgnoreCase);

    private static void AddHeader(TableLayoutPanel grid, string text, int column, int row)
    {
        grid.Controls.Add(new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold)
        }, column, row);
    }

    private void ApplyDefaults()
    {
        var defaults = GestureActions.CreateDefaultMap();
        foreach (var pattern in GesturePatterns.All)
        {
            if (!_actionBoxes.TryGetValue(pattern.Key, out var combo)) continue;
            var action = defaults.TryGetValue(pattern.Key, out var value) ? value : GestureActions.None;
            for (var i = 0; i < combo.Items.Count; i++)
            {
                if (combo.Items[i] is ComboItem item && item.Value.Equals(action, StringComparison.OrdinalIgnoreCase))
                {
                    combo.SelectedIndex = i;
                    break;
                }
            }
        }
    }

    private sealed class ComboItem
    {
        public ComboItem(string value, string text)
        {
            Value = value;
            Text = text;
        }

        public string Value { get; }
        public string Text { get; }
        public override string ToString() => Text;
    }
}
