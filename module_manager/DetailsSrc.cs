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
    }
}
