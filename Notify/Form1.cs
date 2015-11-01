using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Management;
using System.Runtime.InteropServices;

namespace Notify
{

    public partial class Form1 : Form
    {
        Timer timer1 = new Timer();
        string refreshdelay;
        string currentadapter;
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        extern static bool DestroyIcon(IntPtr handle);

        public Form1()
        {

            InitializeComponent();

            refreshdelay = Notify.Properties.Settings.Default.RefreshDelay;
            currentadapter = Notify.Properties.Settings.Default.CurrentAdapter;
            this.textBox1.Text = refreshdelay;
            IEnumerable<string> adapters = GetAdapters();
            this.listBox1.DataSource = adapters;
            this.listBox1.SelectedItem = adapters.First();


        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            string actualdata = string.Empty;
            char[] entereddata = textBox1.Text.ToCharArray();
            foreach (char aChar in entereddata.AsEnumerable())
            {
                if (Char.IsDigit(aChar))
                {
                    actualdata = actualdata + aChar;
                    // MessageBox.Show(aChar.ToString());
                }
                else
                {
                    MessageBox.Show(aChar + " is not numeric");
                    actualdata.Replace(aChar, ' ');
                    actualdata.Trim();
                }
            }
            textBox1.Text = actualdata;
            refreshdelay = actualdata;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // When the program begins, show the balloon on the icon for one second.
            //notifyIcon1.ShowBalloonTip(1000);
            this.Activate();
            //this.Show();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (sender == timer1)
            {
                DrawString(GetSpeed(currentadapter));
            }
        }

        private void DrawString(string stringy)
        {

            Bitmap bm = new Bitmap(32, 32);
            using (Font font = new Font("Helvetica", 8, GraphicsUnit.Point))
            using (Graphics g = Graphics.FromImage(bm))
            {
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                StringFormat stringFormat = new StringFormat()
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Near
                };
                Rectangle rect = new Rectangle(0, 0, bm.Width, bm.Height);
                // measure how large the text is on the Graphics object with the current font size
                SizeF s = g.MeasureString(stringy, font);
                // calculate how to scale the font to make the text fit
                float fontScale = Math.Max(s.Width / rect.Width, s.Height / rect.Height);
                using (Font fontForDrawing = new Font(font.FontFamily, font.SizeInPoints / fontScale, GraphicsUnit.Point))
                {
                    g.DrawString(stringy, fontForDrawing, Brushes.Black, rect, stringFormat);
                }
                notifyIcon1.Icon = Icon.FromHandle(bm.GetHicon());
                DestroyIcon(notifyIcon1.Icon.Handle);



            }
        }

        private void Close_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            // Show the form when the user double clicks on the notify icon. 

            // Set the WindowState to normal if the form is minimized. 
            if (this.WindowState == FormWindowState.Minimized)
                this.WindowState = FormWindowState.Normal;

            // Activate the form. 
            this.Activate();
            this.Show();
            timer1.Stop();

        }

        private void notifyIcon1_MouseDoubleClick_1(object sender, MouseEventArgs e)
        {
            // Show the form when the user double clicks on the notify icon. 

            // Set the WindowState to normal if the form is minimized. 
            if (this.WindowState == FormWindowState.Minimized)
                this.WindowState = FormWindowState.Normal;

            // Activate the form. 
            this.Activate();
            this.Show();
            timer1.Stop();
        }

        private string GetSpeed(string index)
        {
            ManagementObjectSearcher query = new ManagementObjectSearcher("Select Name,CurrentBandwidth from Win32_PerfFormattedData_Tcpip_NetworkInterface where Name ='" + index + "'");
            int numval = 0;
            ManagementObjectCollection queryCollection = query.Get();

            foreach (ManagementObject queryObj in queryCollection)
            {
                numval = Convert.ToInt32(queryObj["CurrentBandwidth"]);
                numval = numval / 1000 / 1000;
            }
            return numval.ToString();

        }

        private IEnumerable<string> GetAdapters()
        {
            List<string> Adapters = new List<string>();
            ManagementObjectSearcher query = new ManagementObjectSearcher("Select Name from Win32_PerfFormattedData_Tcpip_NetworkInterface");

            ManagementObjectCollection queryCollection = query.Get();
            foreach (ManagementObject queryObj in queryCollection)
            {
                Adapters.Add(queryObj["Name"].ToString());
            }
            return Adapters;
        }


        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            currentadapter = this.listBox1.SelectedItem.ToString();
        }

        private void Form1_Closing_Event(object sender, FormClosingEventArgs e)
        {
            Notify.Properties.Settings.Default.RefreshDelay = refreshdelay;
            Notify.Properties.Settings.Default.CurrentAdapter = currentadapter;
            Notify.Properties.Settings.Default.Save();
        }

        private void NotifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            {
                // Show the form when the user double clicks on the notify icon. 

                // Set the WindowState to normal if the form is minimized. 
                if (this.WindowState == FormWindowState.Minimized)
                    this.WindowState = FormWindowState.Normal;

                // Activate the form. 
                this.Activate();
                timer1.Stop();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {

            this.Hide();
            DrawString(GetSpeed(currentadapter));
            timer1 = new Timer();

            // Setup timer
            timer1.Interval = (Convert.ToInt16(refreshdelay) * 1000); //1000ms = 1sec
            timer1.Tick += new EventHandler(timer1_Tick);
            timer1.Start();
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            timer1.Stop();
        }


    }
}

