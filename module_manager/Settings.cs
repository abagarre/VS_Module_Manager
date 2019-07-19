using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace module_manager
{
    public partial class Settings : Form
    {
        Config config;
        public Settings()
        {
            InitializeComponent();
        }

        private void Settings_Load(object sender, EventArgs e)
        {
            config = new Config();
            string json;
            json = File.ReadAllText(config.GetConfigPath());
            JObject conf = JObject.Parse(json);
            if((string) conf["client"] == "smartgit")
            {
                radioButton1.Checked = true;
            }
            else if((string)conf["client"] == "sourcetree")
            {
                radioButton2.Checked = true;
            }
        }

        private void MetroButton1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string fileName = openFileDialog1.FileName;
                textBox4.Text = fileName;
            }
        }

        private void OpenFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void MetroButton5_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string fileName = openFileDialog1.FileName;
                textBox3.Text = fileName;
            }
        }

        private void MetroButton4_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void MetroButton3_Click(object sender, EventArgs e)
        {
            string json;
            json = File.ReadAllText(config.GetSettingsPath());
            JObject conf = JObject.Parse(json);
            conf["smartgit"] = textBox4.Text;
            conf["sourcetree"] = textBox3.Text;
            /*string servs = File.ReadAllText(config.GetServersPath());
            JObject servlist = JObject.Parse(servs);
            foreach (JObject obj in servlist["servers"])
            {
                if ((string)obj["name"] == name)
                {
                    conf["type"] = obj["type"];
                    conf["url"] = obj["url"];
                    conf["username"] = obj["username"];
                }
            }*/
            File.WriteAllText(config.GetSettingsPath(), conf.ToString());

            var checkedButton = panel3.Controls.OfType<RadioButton>().FirstOrDefault(r => r.Checked);
            json = File.ReadAllText(config.GetConfigPath());
            JObject confg = JObject.Parse(json);
            confg["client"] = checkedButton.Text.ToLower();
            File.WriteAllText(config.GetConfigPath(), confg.ToString());

            this.Close();
        }

        private void RadioButton1_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
