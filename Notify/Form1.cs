using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Management;
using System.Reflection;

namespace Notify
{
    public partial class Form1 : Form
    {
        private Timer refreshTimer = new Timer();
        private string refreshdelay;
        private string currentadapter;
        private string colour;
        public Dictionary<string, string> values = new Dictionary<string, string>();

        public Form1()
        {

            InitializeComponent();

            refreshdelay = Properties.Settings.Default.RefreshDelay;
            currentadapter = Properties.Settings.Default.CurrentAdapter;
            colour = Properties.Settings.Default.Colour;
            refreshDelayText.Text = refreshdelay;
            adapterCombo.DataSource = GetAdapters();
            adapterCombo.SelectedItem = currentadapter;
            colourComboBox.DataSource = GetColours();
            colourComboBox.SelectedItem = colour;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            string actualdata = string.Empty;
            char[] entereddata = refreshDelayText.Text.ToCharArray();
            foreach (char aChar in entereddata.AsEnumerable())
            {
                if (Char.IsDigit(aChar))
                {
                    actualdata = actualdata + aChar;
                }
                else
                {
                    MessageBox.Show(string.Format("The entered character {0} is not numeric, try again!",aChar),"Refresh Delay");
                    actualdata.Replace(aChar, ' ');
                    actualdata.Trim();
                }
            }
            refreshDelayText.Text = actualdata;
            refreshdelay = actualdata;
            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Activate();
        }


        private void timer1_Tick(object sender, EventArgs e)
        {
            if (sender == refreshTimer)
            {
                string tempSpeed = GetSpeed(currentadapter);
                DrawString(tempSpeed,colour);
            }
        }

        private void DrawString(string stringylong, string brushColour)
        {
            string stringy = stringylong.Split('.')[0];
            Bitmap bm = new Bitmap(32, 32);
            var brushColor = Color.FromName(brushColour);
            var brush = new SolidBrush(brushColor);
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
                    
                    g.DrawString(stringy, fontForDrawing, brush, rect, stringFormat);
                }
                notifyIcon1.Icon = Icon.FromHandle(bm.GetHicon());
                this.notifyIcon1.Text = string.Format("Current Network Speed is {0}Mbps", stringylong);
                Win32.DestroyIcon(notifyIcon1.Icon.Handle);
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
            refreshTimer.Stop();

        }


        private string GetSpeed(string index)
        {
            ManagementObjectSearcher query = new ManagementObjectSearcher("select Name,Speed from Win32_NetworkAdapter where Name ='" + index + "'");
            decimal numval = 0;
            ManagementObjectCollection queryCollection = query.Get();
            try
            {
                foreach (ManagementObject queryObj in queryCollection)
                {
                    numval = Convert.ToDecimal(queryObj["Speed"]);
                    numval = numval / 1000 / 1000;

                }
                return numval.ToString();

            }
            
            catch
            {
                return "0";
            }
        }

        private IEnumerable<string> GetAdapters()
        {
            List<string> Adapters = new List<string>();
            ManagementObjectSearcher query = new ManagementObjectSearcher("select Name from Win32_NetworkAdapter");

            ManagementObjectCollection queryCollection = query.Get();
            foreach (ManagementObject queryObj in queryCollection)
            {
                Adapters.Add(queryObj["Name"].ToString());
            }
            return Adapters;
        }

        private IEnumerable<string> GetColours()
        {

            Type colorType = typeof(System.Drawing.Color);
            List<string> colorNames = new List<string>();

            PropertyInfo[] propInfoList = colorType.GetProperties(BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.Public);

            foreach(PropertyInfo propinf in propInfoList)
            {
                colorNames.Add(propinf.Name.ToString());
            }
            colorNames.Remove("Transparent");

            return colorNames;

        }



        private void adapterCombo_SelectionChangeCommitted(object sender, EventArgs e)
        {
            currentadapter = this.adapterCombo.SelectedItem.ToString();
        }

        private void colourComboBox_SelectionChangeCommitted(object sender, EventArgs e)
        {
            colour = this.colourComboBox.SelectedItem.ToString();
        }


        private void Form1_Closing_Event(object sender, FormClosingEventArgs e)
        {

            Notify.Properties.Settings.Default.RefreshDelay = refreshdelay;
            Notify.Properties.Settings.Default.CurrentAdapter = currentadapter;
            Notify.Properties.Settings.Default.Colour = colour;
            Notify.Properties.Settings.Default.Save();

        }

        private void NotifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            
                if (e.Button == System.Windows.Forms.MouseButtons.Left)
                {
                    // Show the form when the user double clicks on the notify icon. 

                    // Set the WindowState to normal if the form is minimized. 
                    if (this.WindowState == FormWindowState.Minimized)
                        this.WindowState = FormWindowState.Normal;

                    // Activate the form. 
                    this.Activate();
                    this.Show();
                    refreshTimer.Stop();
                }
          
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int testInt = Convert.ToInt32(refreshdelay);
            if (!(testInt > 0) || !(testInt < 32767))
            {
                MessageBox.Show("Value must be between 1 and 32767","Refresh Delay");
                refreshdelay = "1";
                refreshDelayText.Text = refreshdelay;
            }
            else
            {
                this.Hide();
                DrawString(GetSpeed(currentadapter).Split('.')[0],colour);
                refreshTimer = new Timer();

                // Setup timer
                refreshTimer.Interval = (Convert.ToInt16(refreshdelay) * 1000); //1000ms = 1sec
                refreshTimer.Tick += new EventHandler(timer1_Tick);
                refreshTimer.Start();
            }
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            refreshTimer.Stop();
        }


    }
}

