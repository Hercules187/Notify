/* Notify - A utility to display the wireless connection speed
    Copyright(C) 2018  Steven Connelly

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.If not, see<https://www.gnu.org/licenses/>.
*/

using System.Collections.Generic;

namespace Notify
{
    partial class Form1
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.Close = new System.Windows.Forms.ToolStripMenuItem();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.refreshDelayText = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.adapterCombo = new System.Windows.Forms.ComboBox();
            this.colourComboBox = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.ContextMenuStrip = this.contextMenuStrip1;
            this.notifyIcon1.Visible = true;
            this.notifyIcon1.MouseClick += new System.Windows.Forms.MouseEventHandler(this.NotifyIcon1_MouseClick);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1,
            this.Close});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(162, 64);
            this.contextMenuStrip1.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStrip1_Opening);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(161, 30);
            this.toolStripMenuItem1.Text = "Settings";
            this.toolStripMenuItem1.Click += new System.EventHandler(this.toolStripMenuItem1_Click);
            // 
            // Close
            // 
            this.Close.Name = "Close";
            this.Close.Size = new System.Drawing.Size(161, 30);
            this.Close.Text = "Close";
            this.Close.Click += new System.EventHandler(this.Close_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(14, 23);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(134, 20);
            this.label1.TabIndex = 2;
            this.label1.Text = "Wireless Adapter:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(14, 63);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(188, 20);
            this.label2.TabIndex = 4;
            this.label2.Text = "Refresh Delay (seconds):";
            // 
            // refreshDelayText
            // 
            this.refreshDelayText.Location = new System.Drawing.Point(298, 58);
            this.refreshDelayText.MaxLength = 5;
            this.refreshDelayText.Name = "refreshDelayText";
            this.refreshDelayText.Size = new System.Drawing.Size(124, 26);
            this.refreshDelayText.TabIndex = 5;
            this.refreshDelayText.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(185, 132);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(84, 31);
            this.button1.TabIndex = 6;
            this.button1.Text = "Done";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // adapterCombo
            // 
            this.adapterCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.adapterCombo.FormattingEnabled = true;
            this.adapterCombo.Location = new System.Drawing.Point(153, 18);
            this.adapterCombo.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.adapterCombo.Name = "adapterCombo";
            this.adapterCombo.Size = new System.Drawing.Size(270, 28);
            this.adapterCombo.TabIndex = 7;
            this.adapterCombo.SelectionChangeCommitted += new System.EventHandler(this.adapterCombo_SelectionChangeCommitted);
            // 
            // colourComboBox
            // 
            this.colourComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.colourComboBox.FormattingEnabled = true;
            this.colourComboBox.Location = new System.Drawing.Point(298, 92);
            this.colourComboBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.colourComboBox.Name = "colourComboBox";
            this.colourComboBox.Size = new System.Drawing.Size(125, 28);
            this.colourComboBox.TabIndex = 8;
            this.colourComboBox.SelectionChangeCommitted += new System.EventHandler(this.colourComboBox_SelectionChangeCommitted);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(14, 100);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(55, 20);
            this.label3.TabIndex = 9;
            this.label3.Text = "Colour";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(463, 246);
            this.ControlBox = false;
            this.Controls.Add(this.label3);
            this.Controls.Add(this.colourComboBox);
            this.Controls.Add(this.adapterCombo);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.refreshDelayText);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "Form1";
            this.Text = "Bandwidth Bandit";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_Closing_Event);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem Close;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox refreshDelayText;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.ComboBox adapterCombo;
        private System.Windows.Forms.ComboBox colourComboBox;
        private System.Windows.Forms.Label label3;
    }
}

