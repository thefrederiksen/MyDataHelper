using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using MyDataHelper.Services;

namespace MyDataHelper.Forms
{
    public partial class StartupErrorForm : Form
    {
        private Label titleLabel;
        private TextBox errorTextBox;
        private Button viewLogButton;
        private Button exitButton;
        private string? _exception;
        
        public StartupErrorForm(string message, Exception? exception)
        {
            InitializeComponent();
            
            titleLabel.Text = message;
            
            if (exception != null)
            {
                _exception = FormatException(exception);
                errorTextBox.Text = _exception;
            }
            else
            {
                errorTextBox.Text = "No additional error information available.";
            }
        }
        
        private void InitializeComponent()
        {
            this.titleLabel = new Label();
            this.errorTextBox = new TextBox();
            this.viewLogButton = new Button();
            this.exitButton = new Button();
            this.SuspendLayout();
            
            // StartupErrorForm
            this.AutoScaleDimensions = new SizeF(8F, 16F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(600, 400);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "StartupErrorForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "MyDataHelper - Startup Error";
            
            // titleLabel
            this.titleLabel.AutoSize = false;
            this.titleLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point);
            this.titleLabel.ForeColor = Color.FromArgb(192, 0, 0);
            this.titleLabel.Location = new Point(12, 12);
            this.titleLabel.Name = "titleLabel";
            this.titleLabel.Size = new Size(576, 60);
            this.titleLabel.TabIndex = 0;
            this.titleLabel.Text = "An error occurred";
            this.titleLabel.TextAlign = ContentAlignment.MiddleCenter;
            
            // errorTextBox
            this.errorTextBox.BackColor = SystemColors.Window;
            this.errorTextBox.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point);
            this.errorTextBox.Location = new Point(12, 80);
            this.errorTextBox.Multiline = true;
            this.errorTextBox.Name = "errorTextBox";
            this.errorTextBox.ReadOnly = true;
            this.errorTextBox.ScrollBars = ScrollBars.Both;
            this.errorTextBox.Size = new Size(576, 270);
            this.errorTextBox.TabIndex = 1;
            this.errorTextBox.WordWrap = false;
            
            // viewLogButton
            this.viewLogButton.Location = new Point(12, 360);
            this.viewLogButton.Name = "viewLogButton";
            this.viewLogButton.Size = new Size(100, 28);
            this.viewLogButton.TabIndex = 2;
            this.viewLogButton.Text = "View Log";
            this.viewLogButton.UseVisualStyleBackColor = true;
            this.viewLogButton.Click += new EventHandler(this.ViewLogButton_Click);
            
            // exitButton
            this.exitButton.DialogResult = DialogResult.OK;
            this.exitButton.Location = new Point(488, 360);
            this.exitButton.Name = "exitButton";
            this.exitButton.Size = new Size(100, 28);
            this.exitButton.TabIndex = 3;
            this.exitButton.Text = "Exit";
            this.exitButton.UseVisualStyleBackColor = true;
            
            // Add controls
            this.Controls.Add(this.exitButton);
            this.Controls.Add(this.viewLogButton);
            this.Controls.Add(this.errorTextBox);
            this.Controls.Add(this.titleLabel);
            
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        
        private string FormatException(Exception ex)
        {
            var text = $"Exception Type: {ex.GetType().FullName}\r\n";
            text += $"Message: {ex.Message}\r\n\r\n";
            text += $"Stack Trace:\r\n{ex.StackTrace}\r\n";
            
            if (ex.InnerException != null)
            {
                text += $"\r\nInner Exception:\r\n";
                text += FormatException(ex.InnerException);
            }
            
            return text;
        }
        
        private void ViewLogButton_Click(object? sender, EventArgs e)
        {
            try
            {
                var logPath = StartupErrorLogger.GetLogPath();
                if (File.Exists(logPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = logPath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    MessageBox.Show("Log file not found.", "Information", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open log file: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}