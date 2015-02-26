namespace ManagedWinapi
{
    partial class ShortcutBox
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
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.ContextMenuStrip ctxMenu;
            System.Windows.Forms.ToolStripMenuItem noneMenuItem;
            System.Windows.Forms.ToolStripSeparator sep1;
            System.Windows.Forms.ToolStripSeparator sep2;
            this.tabMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.returnMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.escMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.prtscMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ctrlMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.altMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.winMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.shiftMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            ctxMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            noneMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            sep1 = new System.Windows.Forms.ToolStripSeparator();
            sep2 = new System.Windows.Forms.ToolStripSeparator();
            ctxMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // ctxMenu
            // 
            ctxMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            noneMenuItem,
            sep1,
            this.tabMenuItem,
            this.returnMenuItem,
            this.escMenuItem,
            this.prtscMenuItem,
            sep2,
            this.ctrlMenuItem,
            this.altMenuItem,
            this.winMenuItem,
            this.shiftMenuItem});
            ctxMenu.Name = "ctxMenu";
            ctxMenu.Size = new System.Drawing.Size(111, 214);
            // 
            // noneMenuItem
            // 
            noneMenuItem.Name = "noneMenuItem";
            noneMenuItem.Size = new System.Drawing.Size(110, 22);
            noneMenuItem.Text = "None";
            noneMenuItem.Click += new System.EventHandler(this.noneMenuItem_Click);
            // 
            // sep1
            // 
            sep1.Name = "sep1";
            sep1.Size = new System.Drawing.Size(107, 6);
            // 
            // tabMenuItem
            // 
            this.tabMenuItem.Name = "tabMenuItem";
            this.tabMenuItem.Size = new System.Drawing.Size(110, 22);
            this.tabMenuItem.Click += new System.EventHandler(this.tabMenuItem_Click);
            // 
            // returnMenuItem
            // 
            this.returnMenuItem.Name = "returnMenuItem";
            this.returnMenuItem.Size = new System.Drawing.Size(110, 22);
            this.returnMenuItem.Click += new System.EventHandler(this.returnMenuItem_Click);
            // 
            // escMenuItem
            // 
            this.escMenuItem.Name = "escMenuItem";
            this.escMenuItem.Size = new System.Drawing.Size(110, 22);
            this.escMenuItem.Click += new System.EventHandler(this.escMenuItem_Click);
            // 
            // prtscMenuItem
            // 
            this.prtscMenuItem.Name = "prtscMenuItem";
            this.prtscMenuItem.Size = new System.Drawing.Size(110, 22);
            this.prtscMenuItem.Click += new System.EventHandler(this.prtscMenuItem_Click);
            // 
            // sep2
            // 
            sep2.Name = "sep2";
            sep2.Size = new System.Drawing.Size(107, 6);
            // 
            // ctrlMenuItem
            // 
            this.ctrlMenuItem.Name = "ctrlMenuItem";
            this.ctrlMenuItem.Size = new System.Drawing.Size(110, 22);
            this.ctrlMenuItem.Click += new System.EventHandler(this.ctrlMenuItem_Click);
            // 
            // altMenuItem
            // 
            this.altMenuItem.Name = "altMenuItem";
            this.altMenuItem.Size = new System.Drawing.Size(110, 22);
            this.altMenuItem.Click += new System.EventHandler(this.altMenuItem_Click);
            // 
            // winMenuItem
            // 
            this.winMenuItem.Name = "winMenuItem";
            this.winMenuItem.Size = new System.Drawing.Size(110, 22);
            this.winMenuItem.Click += new System.EventHandler(this.winMenuItem_Click);
            // 
            // shiftMenuItem
            // 
            this.shiftMenuItem.Name = "shiftMenuItem";
            this.shiftMenuItem.Size = new System.Drawing.Size(110, 22);
            this.shiftMenuItem.Click += new System.EventHandler(this.shiftMenuItem_Click);
            // 
            // ShortcutBox
            // 
            this.ContextMenuStrip = ctxMenu;
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.ShortcutBox_KeyUp);
            this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.ShortcutBox_KeyPress);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ShortcutBox_KeyDown);
            ctxMenu.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ToolStripMenuItem tabMenuItem;
        private System.Windows.Forms.ToolStripMenuItem returnMenuItem;
        private System.Windows.Forms.ToolStripMenuItem escMenuItem;
        private System.Windows.Forms.ToolStripMenuItem prtscMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ctrlMenuItem;
        private System.Windows.Forms.ToolStripMenuItem altMenuItem;
        private System.Windows.Forms.ToolStripMenuItem winMenuItem;
        private System.Windows.Forms.ToolStripMenuItem shiftMenuItem;
    }
}
