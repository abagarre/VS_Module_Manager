using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        }

        private void MetroButton1_Click(object sender, EventArgs e)
        {
            if(textBox1.Text != "" && textBox2.Text != "" && textBox3.Text != "" && textBox4.Text != "")
            {
                if(functions.SavePassword(textBox4.Text,textBox1.Text))
                {
                    var checkedButton = panel1.Controls.OfType<RadioButton>().FirstOrDefault(r => r.Checked);
                    config.AddServer(checkedButton.Text.ToLower(), textBox1.Text, textBox2.Text, textBox3.Text, "private");
                    this.Close();
                }

            }
        }

        private void RadioButton1_CheckedChanged(object sender, EventArgs e)
        {
            panel2.Enabled = true;
            metroButton1.Enabled = true;
            button1.Visible = false;
            metroLabel5.Text = "Mot de passe";
        }

        private void MetroButton2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void RadioButton2_CheckedChanged(object sender, EventArgs e)
        {
            panel2.Enabled = true;
            metroButton1.Enabled = true;
            button1.Visible = false;
            metroLabel5.Text = "Mot de passe";
        }

        private void RadioButton3_CheckedChanged(object sender, EventArgs e)
        {
            panel2.Enabled = true;
            metroButton1.Enabled = true;
            button1.Visible = true;
            metroLabel5.Text = "Token";
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            Process.Start("https://docs.microsoft.com/en-us/azure/devops/organizations/accounts/use-personal-access-tokens-to-authenticate?view=azure-devops");
        }
    }
}
