using System;
using System.Drawing;
using System.Windows.Forms;

namespace VeilView;

internal sealed class OpacitySettingsDialog : Form
{
    private readonly TrackBar _slider = new();
    private readonly Label _valueLabel = new();

    public OpacitySettingsDialog(int currentOpacityPercent)
    {
        Text = "불투명도 설정";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(440, 190);
        Padding = new Padding(14);
        ShowInTaskbar = false;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var guideLabel = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            Text = "불투명도 기준으로 조절합니다. 100%는 완전 불투명, 30%는 많이 투명한 상태입니다."
        };

        _slider.Dock = DockStyle.Fill;
        _slider.Minimum = 30;
        _slider.Maximum = 100;
        _slider.TickFrequency = 10;
        _slider.SmallChange = 1;
        _slider.LargeChange = 10;
        _slider.Value = ClampOpacityPercent(currentOpacityPercent);
        _slider.ValueChanged += (_, _) =>
        {
            UpdateValueLabel();
            OpacityPercentChanged?.Invoke(SelectedOpacityPercent);
        };

        _valueLabel.Dock = DockStyle.Fill;
        _valueLabel.AutoSize = true;
        _valueLabel.TextAlign = ContentAlignment.MiddleCenter;
        _valueLabel.Font = new Font(SystemFonts.MessageBoxFont, FontStyle.Bold);

        var rangeLabel = new Label
        {
            Text = "30% = 많이 투명    |    100% = 완전 불투명",
            AutoSize = true,
            Dock = DockStyle.Fill,
            ForeColor = Color.DimGray
        };

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true
        };

        var okButton = new Button { Text = "저장", Width = 82, DialogResult = DialogResult.OK };
        var cancelButton = new Button { Text = "취소", Width = 82, DialogResult = DialogResult.Cancel };
        var opaqueButton = new Button { Text = "100%", Width = 82 };
        var middleButton = new Button { Text = "70%", Width = 82 };
        var transparentButton = new Button { Text = "30%", Width = 82 };

        opaqueButton.Click += (_, _) => _slider.Value = 100;
        middleButton.Click += (_, _) => _slider.Value = 70;
        transparentButton.Click += (_, _) => _slider.Value = 30;

        buttonPanel.Controls.Add(okButton);
        buttonPanel.Controls.Add(cancelButton);
        buttonPanel.Controls.Add(opaqueButton);
        buttonPanel.Controls.Add(middleButton);
        buttonPanel.Controls.Add(transparentButton);

        root.Controls.Add(guideLabel, 0, 0);
        root.Controls.Add(_slider, 0, 1);
        root.Controls.Add(_valueLabel, 0, 2);
        root.Controls.Add(rangeLabel, 0, 3);
        root.Controls.Add(buttonPanel, 0, 4);

        Controls.Add(root);
        AcceptButton = okButton;
        CancelButton = cancelButton;

        UpdateValueLabel();
    }

    public event Action<int>? OpacityPercentChanged;

    public int SelectedOpacityPercent => _slider.Value;

    private void UpdateValueLabel()
    {
        var opacity = SelectedOpacityPercent;
        var transparency = 100 - opacity;
        var hint = opacity switch
        {
            >= 95 => "완전 불투명",
            >= 75 => "약간 투명",
            >= 55 => "중간 투명",
            _ => "많이 투명"
        };

        _valueLabel.Text = $"불투명도 {opacity}% / 투명도 {transparency}% ({hint})";
    }

    private static int ClampOpacityPercent(int value) => Math.Clamp(value, 30, 100);
}
