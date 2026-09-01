namespace PosSystem.UI
{
    partial class frmTerminalActivation
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        // UI Control Declarations
        private System.Windows.Forms.TextBox txtTAC;
        private System.Windows.Forms.Button btnActivate;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.txtTAC = new System.Windows.Forms.TextBox();
            this.btnActivate = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // txtTAC
            // 
            this.txtTAC.Location = new System.Drawing.Point(30, 30);
            this.txtTAC.Name = "txtTAC";
            this.txtTAC.Size = new System.Drawing.Size(200, 22);
            this.txtTAC.TabIndex = 0;
            // 
            // btnActivate
            // 
            this.btnActivate.Location = new System.Drawing.Point(30, 70);
            this.btnActivate.Name = "btnActivate";
            this.btnActivate.Size = new System.Drawing.Size(100, 30);
            this.btnActivate.TabIndex = 1;
            this.btnActivate.Text = "Activate";
            this.btnActivate.UseVisualStyleBackColor = true;
            this.btnActivate.Click += new System.EventHandler(this.btnActivate_Click);
            // 
            // frmTerminalActivation
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(300, 150);
            this.Controls.Add(this.txtTAC);
            this.Controls.Add(this.btnActivate);
            this.Name = "frmTerminalActivation";
            this.Text = "Terminal Activation";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}