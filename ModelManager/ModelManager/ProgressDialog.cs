using System.Threading;
// ...existing code...
public partial class ProgressDialog : Form
{
    public ProgressBar ProgressBar { get; private set; }
    public Label StatusLabel { get; private set; }
    public Button CancelButton { get; private set; }
    public CancellationTokenSource CancellationTokenSource { get; private set; }

    public ProgressDialog()
    {
        this.Text = "Loading Models";
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.StartPosition = FormStartPosition.CenterParent;
        this.Width = 500;
        this.Height = 160;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.ControlBox = false;
        this.ShowInTaskbar = false;
        this.ProgressBar = new ProgressBar { Location = new System.Drawing.Point(20, 20), Width = 440, Height = 20 };
        this.StatusLabel = new Label { Location = new System.Drawing.Point(20, 50), Width = 440, Height = 20 };
        this.CancelButton = new Button { Text = "Cancel", Location = new System.Drawing.Point(380, 80), Width = 80, Height = 30 };
        this.CancelButton.Click += (s, e) => { CancellationTokenSource?.Cancel(); };
        this.Controls.Add(ProgressBar);
        this.Controls.Add(StatusLabel);
        this.Controls.Add(CancelButton);
        this.CancellationTokenSource = new CancellationTokenSource();
    }
}
