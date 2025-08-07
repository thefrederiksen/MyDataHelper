using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace MyDataHelper.Forms
{
    public partial class StartupForm : Form
    {
        private Label statusLabel = null!;
        private ProgressBar progressBar = null!;
        private PictureBox logoPictureBox = null!;
        
        public StartupForm()
        {
            InitializeComponent();
        }
        
        private void InitializeComponent()
        {
            this.statusLabel = new Label();
            this.progressBar = new ProgressBar();
            this.logoPictureBox = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.logoPictureBox)).BeginInit();
            this.SuspendLayout();
            
            // StartupForm
            this.AutoScaleDimensions = new SizeF(8F, 16F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.BackColor = Color.White;
            this.ClientSize = new Size(500, 300);
            this.ControlBox = false;
            this.FormBorderStyle = FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "StartupForm";
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "MyDataHelper";
            this.TopMost = true;
            
            // logoPictureBox
            this.logoPictureBox.Location = new Point(150, 50);
            this.logoPictureBox.Name = "logoPictureBox";
            this.logoPictureBox.Size = new Size(200, 100);
            this.logoPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            this.logoPictureBox.TabIndex = 0;
            this.logoPictureBox.TabStop = false;
            
            // Try to load logo
            try
            {
                var logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "MyDataHelper Logo.png");
                if (File.Exists(logoPath))
                {
                    this.logoPictureBox.Image = Image.FromFile(logoPath);
                }
            }
            catch { /* Ignore if logo not found */ }
            
            // statusLabel
            this.statusLabel.AutoSize = false;
            this.statusLabel.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            this.statusLabel.ForeColor = Color.FromArgb(64, 64, 64);
            this.statusLabel.Location = new Point(50, 180);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new Size(400, 30);
            this.statusLabel.TabIndex = 1;
            this.statusLabel.Text = "Starting MyDataHelper...";
            this.statusLabel.TextAlign = ContentAlignment.MiddleCenter;
            
            // progressBar
            this.progressBar.Location = new Point(50, 220);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new Size(400, 23);
            this.progressBar.Style = ProgressBarStyle.Continuous;
            this.progressBar.TabIndex = 2;
            
            // Add controls
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.logoPictureBox);
            
            ((System.ComponentModel.ISupportInitialize)(this.logoPictureBox)).EndInit();
            this.ResumeLayout(false);
        }
        
        public void UpdateStatus(string status, int progress)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateStatus(status, progress)));
                return;
            }
            
            this.statusLabel.Text = status;
            this.progressBar.Value = Math.Min(Math.Max(progress, 0), 100);
            this.Refresh();
        }
        
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            // Draw border
            using (var pen = new Pen(Color.FromArgb(200, 200, 200), 1))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
            }
        }
    }
}