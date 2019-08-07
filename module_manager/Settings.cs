//============================================================================//
//                              SETTINGS FORM                                 //
//                                                                            //
// - Settings form                                                            //
//============================================================================//

using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace module_manager
{
    public partial class Settings : Form
    {
        Config config;

        public Settings()
        {
            InitializeComponent();
            Icon = Icon.ExtractAssociatedIcon("logo.ico");
        }

        private void Settings_Load(object sender, EventArgs e)
        {
            config = new Config();
            string json;
            json = File.ReadAllText(config.GetConfigPath());
            JObject conf = JObject.Parse(json);
            if((string) conf["client"] == "smartgit")
                radioButton1.Checked = true;
            else if((string)conf["client"] == "sourcetree")
                radioButton2.Checked = true;
            else if ((string)conf["client"] == "dossierlocal")
                radioButton3.Checked = true;
            textBox4.Text = config.GetSmartGitRepo();
            textBox3.Text = config.GetSourceTreeRepo();
            textBox1.Text = config.GetLocalRepo();
        }

        private void MetroButton1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string fileName = openFileDialog1.FileName;
                textBox4.Text = fileName;
            }
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
            Close();
        }

        private void MetroButton3_Click(object sender, EventArgs e)
        {
            string json;
            json = File.ReadAllText(config.GetSettingsPath());
            JObject conf = JObject.Parse(json);
            conf["smartgit"] = textBox4.Text;
            conf["sourcetree"] = textBox3.Text;
            conf["local"] = textBox1.Text;
            File.WriteAllText(config.GetSettingsPath(), conf.ToString());
            var checkedButton = panel3.Controls.OfType<RadioButton>().FirstOrDefault(r => r.Checked);
            json = File.ReadAllText(config.GetConfigPath());
            JObject confg = JObject.Parse(json);
            confg["client"] = checkedButton.Text.ToLower().Replace(" ","");
            File.WriteAllText(config.GetConfigPath(), confg.ToString());
            Close();
        }

        private void MetroButton2_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                string path = folderBrowserDialog1.SelectedPath;
                textBox1.Text = path;
            }
        }
    }
}
