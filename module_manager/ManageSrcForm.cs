//============================================================================//
//                              MANAGE SOURCES                                //
//                                                                            //
// - List all sources                                                         //
// - Possibility to edit and add sources                                      //
//============================================================================//

using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace module_manager
{
    public partial class ManageSrcForm : Form
    {
        Config config;
        public ManageSrcForm()
        {
            InitializeComponent();
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
            DetailsSrc frm = new DetailsSrc(args);
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

        private void MetroButton3_Click(object sender, EventArgs e)
        {
            if(MessageBox.Show("Êtes-vous sûr de vouloir réinitialiser les sources enregistrées ? Vous devrez les ajouter manuellement pour les retrouver.", "Réinitialisation",MessageBoxButtons.YesNo,MessageBoxIcon.Question) == DialogResult.Yes)
            {
                string json;
                json = File.ReadAllText(config.GetServersPath());
                JObject conf = JObject.Parse(json);
                JArray list = (JArray)conf["servers"];
                foreach (JObject serv in list)
                {
                    if (File.Exists(Path.Combine(config.GetAppData(), ".cred" + serv["name"])))
                        File.Delete(Path.Combine(config.GetAppData(), ".cred" + serv["name"]));
                }
                int counter = list.Count();
                for(int i = 0; i<counter; i++)
                {
                    list[0].Remove();
                }
                File.WriteAllText(config.GetServersPath(), conf.ToString());
                Close();
            }
        }
    }
}
