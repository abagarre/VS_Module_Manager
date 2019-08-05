//============================================================================//
//                              MANAGE SOURCES                                //
//                                                                            //
// - List all sources                                                         //
// - Possibility to edit and add sources                                      //
//============================================================================//

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace module_manager
{
    public partial class ManageSrcForm : Form
    {

        Functions functions;
        Config config;
        public ManageSrcForm()
        {
            InitializeComponent();
            functions = new Functions();
            config = new Config();
            Icon = Icon.ExtractAssociatedIcon("logo.ico");
        }

        private void Form6_Load(object sender, EventArgs e)
        {
            string json = config.DispServerList();
            JObject serv = JObject.Parse(json);
            foreach (JObject obj in serv["servers"])
            {
                FlowLayoutPanel panel = new FlowLayoutPanel();
                panel.FlowDirection = FlowDirection.TopDown;
                panel.BackColor = SystemColors.MenuBar;
                PictureBox picture = new PictureBox()
                {
                    ImageLocation = config.GetAppData() + (string)obj["type"] + ".png",
                    Anchor = AnchorStyles.None
                };
                panel.Controls.Add(picture);
                panel.Width = picture.Width;
                panel.AutoSize = true;
                panel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                panel.Padding = new Padding(10);
                panel.Controls.Add(new MetroFramework.Controls.MetroLabel() { Text = (string) obj["name"], Width = picture.Width, TextAlign = ContentAlignment.MiddleCenter, UseCustomBackColor = true, BackColor = SystemColors.MenuBar } );
                MetroFramework.Controls.MetroButton buttonDetails = new MetroFramework.Controls.MetroButton()
                {
                    Text = "Détails",
                    Name = (string)obj["name"],
                    Anchor = AnchorStyles.None,
                    Width = picture.Width
                };
                buttonDetails.Click += EditSourceClick;
                panel.Controls.Add(buttonDetails);
                flowLayoutPanel1.Controls.Add(panel);
            }
        }

        private void EditSourceClick(object sender, EventArgs e)
        {
            string name = (sender as Button).Name;
            string[] args = { name };
            var frm = new DetailsSrc(args);
            frm.Location = this.Location;
            frm.StartPosition = FormStartPosition.Manual;
            frm.Show();
            frm.FormClosed += RefreshForm;
        }

        private void MetroButton2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void MetroButton1_Click(object sender, EventArgs e)
        {
            var frm = new AddSource();
            frm.Location = this.Location;
            frm.StartPosition = FormStartPosition.Manual;
            frm.Show();
            frm.FormClosed += RefreshForm;
        }

        private void RefreshForm(object sender, FormClosedEventArgs e)
        {
            flowLayoutPanel1.Controls.Clear();
            Form6_Load(sender, e);
        }
    }
}
