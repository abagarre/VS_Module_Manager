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

        }

        private void MetroButton2_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
