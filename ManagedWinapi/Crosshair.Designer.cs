namespace ManagedWinapi
{
    partial class Crosshair
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
			this.dragger = new System.Windows.Forms.PictureBox();
			((System.ComponentModel.ISupportInitialize)(this.dragger)).BeginInit();
			this.SuspendLayout();
			// 
			// dragger
			// 
			this.dragger.Cursor = System.Windows.Forms.Cursors.Cross;
			this.dragger.Dock = System.Windows.Forms.DockStyle.Fill;
			this.dragger.Location = new System.Drawing.Point(0, 0);
			this.dragger.Name = "dragger";
			this.dragger.Size = new System.Drawing.Size(36, 36);
			this.dragger.TabIndex = 0;
			this.dragger.TabStop = false;
			this.dragger.MouseMove += new System.Windows.Forms.MouseEventHandler(this.dragger_MouseMove);
			this.dragger.MouseDown += new System.Windows.Forms.MouseEventHandler(this.dragger_MouseDown);
			this.dragger.MouseUp += new System.Windows.Forms.MouseEventHandler(this.dragger_MouseUp);
			// 
			// Crosshair
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.dragger);
			this.Name = "Crosshair";
			this.Size = new System.Drawing.Size(36, 36);
			((System.ComponentModel.ISupportInitialize)(this.dragger)).EndInit();
			this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox dragger;
    }
}
