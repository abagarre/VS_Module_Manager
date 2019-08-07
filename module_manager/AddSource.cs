//============================================================================//
//                              ADD SOURCE                                    //
//                                                                            //
// - Choose between Gitblit, Bitbucket and Azure DevOps                       //
// - Fill in name, url, username and password/token                           //
// - Add source to list                                                       //
//============================================================================//

using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace module_manager
{
    public partial class AddSource : Form
    {
        Config config;
        Functions functions;
        public AddSource()
        {
            InitializeComponent();
            config = new Config();
            functions = new Functions();
            Icon = Icon.ExtractAssociatedIcon("logo.ico");
        }

        /// <summary>
        /// Validate and add source
        /// </summary>
        private void MetroButton1_Click(object sender, EventArgs e)
        {
            var checkedButton = panel1.Controls.OfType<RadioButton>().FirstOrDefault(r => r.Checked);
            if (textBox1.Text != "" && textBox2.Text != "" && textBox3.Text != "" && textBox4.Text != "")
            {
                if (functions.SavePassword(textBox4.Text,textBox1.Text))
                {
                    if(config.AddServer(checkedButton.Text.ToLower(), textBox1.Text, textBox2.Text, textBox3.Text))
                        Close();
                    else
                        MessageBox.Show("Un serveur de ce nom existe déjà", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                    MessageBox.Show("Mot de passe incorrect", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if(textBox4.Text == "")
            {
                if (config.AddServer(checkedButton.Text.ToLower(), textBox1.Text, textBox2.Text, textBox3.Text))
                    Close();
                else
                    MessageBox.Show("Un serveur de ce nom existe déjà", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // GitBlit radio button
        private void RadioButton1_CheckedChanged(object sender, EventArgs e)
        {
            panel2.Enabled = true;
            metroButton1.Enabled = true;
            button1.Visible = false;
            metroLabel5.Text = "Mot de passe";
            textBox2.Text = "{address}:{port}/";
        }

        private void MetroButton2_Click(object sender, EventArgs e)
        {
            Close();
        }

        // BitBucket radio button
        private void RadioButton2_CheckedChanged(object sender, EventArgs e)
        {
            panel2.Enabled = true;
            metroButton1.Enabled = true;
            button1.Visible = false;
            metroLabel5.Text = "Mot de passe";
            textBox2.Text = "https://bitbucket.org/{organization}/";
        }

        // Devops radio button
        private void RadioButton3_CheckedChanged(object sender, EventArgs e)
        {
            panel2.Enabled = true;
            metroButton1.Enabled = true;
            button1.Visible = true;
            metroLabel5.Text = "Token";
            textBox2.Text = "https://dev.azure.com/{organization}/{project}/";
        }

        // Details on devops token
        private void Button1_Click(object sender, EventArgs e)
        {
            Process.Start("https://docs.microsoft.com/en-us/azure/devops/organizations/accounts/use-personal-access-tokens-to-authenticate?view=azure-devops");
        }

        private void AddSource_Load(object sender, EventArgs e)
        {
        }
    }
}
