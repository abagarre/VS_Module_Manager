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
    public partial class NewPassword : Form
    {

        internal string pass = "";
        public NewPassword()
        {
            InitializeComponent();
        }

        private void MetroButton1_Click(object sender, EventArgs e)
        {
            if(textBox1.Text == textBox2.Text && MessageBox.Show("Le mot de passe n'est pas sauvegardé ailleurs que dans votre mémoire, il sera impossible de le récupérer si vous l'oubliez. \nContinuer ?","Attention",MessageBoxButtons.YesNo,MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                pass = textBox1.Text;
                this.Close();
            }
        }

        private void MetroButton2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void NewPassword_Load(object sender, EventArgs e)
        {

        }
    }
}
