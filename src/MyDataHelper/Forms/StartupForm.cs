using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace MyDataHelper.Forms
{
    public partial class StartupForm : Form
    {
        private Label statusLabel = null!;
        private ProgressBar progressBar = null!;
        private PictureBox logoPictureBox = null!;
        private System.Windows.Forms.Timer fadeTimer = null!;
        private Label titleLabel = null!;
        private Label versionLabel = null!;
        private Panel progressPanel = null!;
        private Label detailsLabel = null!;
        
        public StartupForm()
        {
            InitializeComponent();
        }
        
        private void InitializeComponent()
        {
            this.statusLabel = new Label();
            this.progressBar = new ProgressBar();
            this.logoPictureBox = new PictureBox();
            this.titleLabel = new Label();
            this.versionLabel = new Label();
            this.fadeTimer = new System.Windows.Forms.Timer();
            this.progressPanel = new Panel();
            this.detailsLabel = new Label();
            ((System.ComponentModel.ISupportInitialize)(this.logoPictureBox)).BeginInit();
            this.SuspendLayout();
            
            // StartupForm
            this.AutoScaleDimensions = new SizeF(8F, 16F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.BackColor = Color.White;
            this.ClientSize = new Size(480, 340);
            this.ControlBox = false;
            this.FormBorderStyle = FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "StartupForm";
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "MyDataHelper";
            this.TopMost = true;
            this.DoubleBuffered = true;
            
            // Set icon
            try
            {
                var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MyDataHelper.ico");
                if (File.Exists(iconPath))
                {
                    this.Icon = new Icon(iconPath);
                }
            }
            catch { /* Ignore if icon not found */ }
            
            // titleLabel
            this.titleLabel.AutoSize = false;
            this.titleLabel.Font = new Font("Segoe UI", 26F, FontStyle.Regular, GraphicsUnit.Point);
            this.titleLabel.ForeColor = Color.FromArgb(32, 32, 32);
            this.titleLabel.Location = new Point(40, 50);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new Size(400, 50);
            this.titleLabel.TabIndex = 3;
            this.titleLabel.Text = "MyDataHelper";
            this.titleLabel.TextAlign = ContentAlignment.MiddleCenter;
            
            // versionLabel
            this.versionLabel.AutoSize = false;
            this.versionLabel.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
            this.versionLabel.ForeColor = Color.FromArgb(108, 117, 125);
            this.versionLabel.Location = new Point(40, 100);
            this.versionLabel.Name = "versionLabel";
            this.versionLabel.Size = new Size(400, 20);
            this.versionLabel.TabIndex = 4;
            this.versionLabel.Text = "Disk Space Analyzer • Version 1.0.0";
            this.versionLabel.TextAlign = ContentAlignment.MiddleCenter;
            
            // logoPictureBox
            this.logoPictureBox.Location = new Point(190, 135);
            this.logoPictureBox.Name = "logoPictureBox";
            this.logoPictureBox.Size = new Size(100, 60);
            this.logoPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            this.logoPictureBox.TabIndex = 0;
            this.logoPictureBox.TabStop = false;
            this.logoPictureBox.Visible = false; // Hide if no icon
            
            // Try to load icon as logo
            try
            {
                var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MyDataHelper.ico");
                if (File.Exists(iconPath))
                {
                    using (var icon = new Icon(iconPath))
                    {
                        this.logoPictureBox.Image = icon.ToBitmap();
                    }
                }
            }
            catch { /* Ignore if icon not found */ }
            
            // progressPanel
            this.progressPanel.BackColor = Color.FromArgb(248, 249, 250);
            this.progressPanel.Location = new Point(0, 210);
            this.progressPanel.Name = "progressPanel";
            this.progressPanel.Size = new Size(480, 130);
            this.progressPanel.TabIndex = 5;
            
            // statusLabel
            this.statusLabel.AutoSize = false;
            this.statusLabel.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point);
            this.statusLabel.ForeColor = Color.FromArgb(32, 32, 32);
            this.statusLabel.Location = new Point(40, 15);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new Size(400, 25);
            this.statusLabel.TabIndex = 1;
            this.statusLabel.Text = "Initializing...";
            this.statusLabel.TextAlign = ContentAlignment.MiddleCenter;
            this.progressPanel.Controls.Add(this.statusLabel);
            
            // detailsLabel
            this.detailsLabel.AutoSize = false;
            this.detailsLabel.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
            this.detailsLabel.ForeColor = Color.FromArgb(108, 117, 125);
            this.detailsLabel.Location = new Point(40, 45);
            this.detailsLabel.Name = "detailsLabel";
            this.detailsLabel.Size = new Size(400, 20);
            this.detailsLabel.TabIndex = 6;
            this.detailsLabel.Text = "";
            this.detailsLabel.TextAlign = ContentAlignment.MiddleCenter;
            this.progressPanel.Controls.Add(this.detailsLabel);
            
            // progressBar
            this.progressBar.Location = new Point(40, 75);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new Size(400, 8);
            this.progressBar.Style = ProgressBarStyle.Continuous;
            this.progressBar.TabIndex = 2;
            this.progressPanel.Controls.Add(this.progressBar);
            
            // fadeTimer
            this.fadeTimer.Interval = 50;
            this.fadeTimer.Tick += new EventHandler(FadeTimer_Tick);
            
            // Add controls
            this.Controls.Add(this.progressPanel);
            this.Controls.Add(this.logoPictureBox);
            this.Controls.Add(this.titleLabel);
            this.Controls.Add(this.versionLabel);
            
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
            
            // Add details for specific stages
            switch (status)
            {
                case "Initializing directories...":
                    this.detailsLabel.Text = "Setting up application folders";
                    break;
                case "Creating web application...":
                    this.detailsLabel.Text = "Preparing web server components";
                    break;
                case "Configuring services...":
                    this.detailsLabel.Text = "Loading application services";
                    break;
                case "Initializing database...":
                    this.detailsLabel.Text = "Preparing data storage";
                    break;
                case "Starting system tray...":
                    this.detailsLabel.Text = "Setting up system integration";
                    break;
                case "Starting web server...":
                    this.detailsLabel.Text = "Launching web interface";
                    break;
                case "Ready! Opening browser...":
                    this.detailsLabel.Text = "Application ready";
                    break;
                default:
                    this.detailsLabel.Text = "";
                    break;
            }
            
            // Smooth progress bar animation
            if (this.progressBar.Value < progress)
            {
                while (this.progressBar.Value < progress)
                {
                    this.progressBar.Value = Math.Min(this.progressBar.Value + 2, progress);
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(10);
                }
            }
            else
            {
                this.progressBar.Value = Math.Min(Math.Max(progress, 0), 100);
            }
            
            this.Refresh();
        }
        
        public void UpdateStatus(string status)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateStatus(status)));
                return;
            }
            
            this.statusLabel.Text = status;
            this.Refresh();
        }
        
        public void CloseWithFade()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => CloseWithFade()));
                return;
            }
            
            fadeTimer.Start();
        }
        
        private void FadeTimer_Tick(object? sender, EventArgs e)
        {
            if (this.Opacity > 0.02)
            {
                this.Opacity -= 0.05;
            }
            else
            {
                fadeTimer.Stop();
                this.Close();
            }
        }
        
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            
            // Draw subtle shadow border
            using (var pen = new Pen(Color.FromArgb(200, 200, 200), 1))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
            }
            
            // Draw a gradient accent bar at the top
            var rect = new Rectangle(0, 0, this.Width, 4);
            using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                rect, 
                Color.FromArgb(0, 120, 212), 
                Color.FromArgb(0, 150, 255), 
                System.Drawing.Drawing2D.LinearGradientMode.Horizontal))
            {
                e.Graphics.FillRectangle(brush, rect);
            }
        }
    }
}