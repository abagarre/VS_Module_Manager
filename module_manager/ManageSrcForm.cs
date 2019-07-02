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
        public ManageSrcForm()
        {
            InitializeComponent();
            functions = new Functions();
        }

        private void Form6_Load(object sender, EventArgs e)
        {
            string json = functions.DispServerList();
            JObject serv = JObject.Parse(json);
            foreach (JObject obj in serv["servers"])
            {
                FlowLayoutPanel panel = new FlowLayoutPanel();
                panel.FlowDirection = FlowDirection.TopDown;
                panel.BackColor = SystemColors.MenuBar;
                PictureBox picture = new PictureBox()
                {
                    ImageLocation = @".\" + (string)obj["type"] + ".png",
                    Anchor = AnchorStyles.None
                };
                panel.Controls.Add(picture);
                panel.Width = picture.Width;
                panel.AutoSize = true;
                panel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                panel.Padding = new Padding(10);
                panel.Controls.Add(new MetroFramework.Controls.MetroLabel() { Text = (string) obj["name"], Width = picture.Width, TextAlign = ContentAlignment.MiddleCenter, BackColor = SystemColors.MenuBar } );
                MetroFramework.Controls.MetroButton buttonDetails = new MetroFramework.Controls.MetroButton()
                {
                    Text = "Détails",
                    Name = (string)obj["name"],
                    Anchor = AnchorStyles.None,
                    Width = picture.Width
            };
                buttonDetails.Click += GetDetailsServer;
                panel.Controls.Add(buttonDetails);
                MetroFramework.Controls.MetroButton buttonSwitch = new MetroFramework.Controls.MetroButton()
                {
                    Text = "Basculer",
                    Name = (string)obj["name"],
                    Anchor = AnchorStyles.None,
                    Width = picture.Width
                };
                buttonSwitch.Click += SwitchServer;
                panel.Controls.Add(buttonSwitch);
                flowLayoutPanel1.Controls.Add(panel);
            }
        }

        private void SwitchServer(object sender, EventArgs e)
        {
            Config.ChangeServer((sender as Button).Name);
            foreach(Control control in flowLayoutPanel1.Controls)
            {
                if(control is FlowLayoutPanel)
                {
                    (control as FlowLayoutPanel).BorderStyle = BorderStyle.None;
                }
            }
            Control ctrl = (sender as Button).Parent;
            (ctrl as FlowLayoutPanel).BorderStyle = BorderStyle.FixedSingle;
        }

        private void GetDetailsServer(object sender, EventArgs e)
        {
            
        }
    }
}
