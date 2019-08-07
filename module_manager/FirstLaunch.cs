//============================================================================//
//                              FIRST LAUNCH FORM                             //
//                                                                            //
// - Ask for all basic informations to make the programm work                 //
//============================================================================//

using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;

namespace module_manager
{
    public partial class FirstLaunch : Form
    {
        Config config;
        Functions functions;
        string checkedItem = "";
        public FirstLaunch()
        {
            InitializeComponent();
            config = new Config();
            functions = new Functions();
            Icon = Icon.ExtractAssociatedIcon("logo.ico");
        }

        // Copie les fichiers nécessaires dans le dossier %appdata%/ModuleManager
        private void FirstLaunch_Load(object sender, EventArgs e)
        {
            string[] files = Directory.GetFiles("data", "*.*", SearchOption.TopDirectoryOnly);
            foreach(string file in files)
            {
                File.Copy(file, config.GetAppData() + file.Substring(file.LastIndexOf(@"\") + 1, file.Length - file.LastIndexOf(@"\") - 1), false);
            }
            label6.Visible = false;
        }

        private void RadioButton1_CheckedChanged(object sender, EventArgs e)
        {
            label4.Text = "Chemin du fichier repositories.xml :";
            textBox1.Text = @"%appdata%\SmartGit\repositories.xml";
            checkedItem = "smartgit";
            metroButton2.Enabled = true;
        }

        private void RadioButton2_CheckedChanged(object sender, EventArgs e)
        {
            label4.Text = "Chemin du fichier opentabs.xml :";
            textBox1.Text = @"%appdata%\..\Local\Atlassian\SourceTree\opentabs.xml";
            checkedItem = "sourcetree";
            metroButton2.Enabled = true;
        }

        private void RadioButton3_CheckedChanged(object sender, EventArgs e)
        {
            label4.Text = "Chemin du dossier :";
            checkedItem = "local";
            textBox1.Text = "";
            metroButton2.Enabled = true;
        }

        /// <summary>
        /// Ajoute le client au fichier des paramètres, ajoute un nouveau serveur
        /// </summary>
        private void MetroButton2_Click(object sender, EventArgs e)
        {
            if(panel1.Visible)
            {
                if(((checkedItem == "smartgit" || checkedItem == "sourcetree") && File.Exists(textBox1.Text))
                    || (checkedItem == "local" && Directory.Exists(textBox1.Text)))
                {
                    string json = File.ReadAllText(config.GetSettingsPath());
                    JObject conf = JObject.Parse(json);
                    conf[checkedItem] = textBox1.Text;
                    File.WriteAllText(config.GetSettingsPath(), conf.ToString());
                    var checkedButton = panel2.Controls.OfType<RadioButton>().FirstOrDefault(r => r.Checked);
                    json = File.ReadAllText(config.GetConfigPath());
                    JObject confg = JObject.Parse(json);
                    confg["client"] = checkedButton.Text.ToLower().Replace(" ", "");
                    File.WriteAllText(config.GetConfigPath(), confg.ToString());
                    panel1.Visible = false;
                    panel3.Visible = true;
                    metroButton2.Enabled = false;
                }
                else
                    label6.Visible = true;
            }
            if(panel3.Visible && textBox5.Text != "" && textBox2.Text != "" && textBox3.Text != "")
            {
                if ((textBox4.Text != "" && functions.SavePassword(textBox4.Text, textBox5.Text)) ||
                        (textBox4.Text == "" && MessageBox.Show("Vous êtes sur le point d'ajouter un serveur ne nécessitant pas de mot de passe. \nContinuer ?", "", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes))
                {
                    var checkedButton = panel5.Controls.OfType<RadioButton>().FirstOrDefault(r => r.Checked);
                    if (config.AddServer(checkedButton.Text.ToLower(), textBox5.Text, textBox2.Text, textBox3.Text))
                    {
                        config.ChangeServer(textBox5.Text);
                        Process.Start("module_manager.exe");
                        this.Close();
                    }
                    else
                        MessageBox.Show("Un serveur de ce nom existe déjà", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void MetroButton1_Click(object sender, EventArgs e)
        {
            if(checkedItem == "local")
            {
                if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
                {
                    string path = folderBrowserDialog1.SelectedPath;
                    textBox1.Text = path;
                }
            }
            else
            {
                if (checkedItem == "sourcetree")
                    openFileDialog1.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    string fileName = openFileDialog1.FileName;
                    textBox1.Text = fileName;
                }
            }
        }

        private void RadioButton7_CheckedChanged(object sender, EventArgs e)
        {
            panel4.Enabled = true;
            metroButton2.Enabled = true;
            button1.Visible = false;
            metroLabel5.Text = "Mot de passe";
            textBox2.Text = "{address}:{port}/";
        }

        private void RadioButton6_CheckedChanged(object sender, EventArgs e)
        {
            panel4.Enabled = true;
            metroButton2.Enabled = true;
            button1.Visible = false;
            metroLabel5.Text = "Mot de passe";
            textBox2.Text = "https://bitbucket.org/{organization}/";
        }

        private void RadioButton5_CheckedChanged(object sender, EventArgs e)
        {
            panel4.Enabled = true;
            metroButton2.Enabled = true;
            button1.Visible = true;
            metroLabel5.Text = "Token";
            textBox2.Text = "https://dev.azure.com/{organization}/{project}/";
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            Process.Start("https://docs.microsoft.com/en-us/azure/devops/organizations/accounts/use-personal-access-tokens-to-authenticate?view=azure-devops");
        }
    }
}
