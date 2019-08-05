//============================================================================//
//                              SOURCE DETAILS                                //
//                                                                            //
// - Load source informations                                                 //
// - Allow edit and delete source                                             //
//============================================================================//

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
    public partial class DetailsSrc : Form
    {
        string name = "";
        Config config;
        public DetailsSrc(string[] args)
        {
            InitializeComponent();
            name = args[0];
            config = new Config();
            Icon = Icon.ExtractAssociatedIcon("logo.ico");
        }

        private void DetailsSrc_Load(object sender, EventArgs e)
        {
            textBox1.Text = name;
            textBox2.Text = config.GetServerUrl(name);
            textBox3.Text = config.GetUserName(name);
        }

        private void MetroButton1_Click(object sender, EventArgs e)
        {
            config.EditServer(name, textBox1.Text, textBox2.Text, textBox3.Text);
            this.Close();
        }

        private void MetroButton2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void MetroButton3_Click(object sender, EventArgs e)
        {
            if(MessageBox.Show("Êtes vous sûr de vouloir supprimer le serveur " + name + " ?","Supprimer", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                config.DeleteServer(name);
                this.Close();
            }  
        }
    }
}
