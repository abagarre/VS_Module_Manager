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
    public partial class Password : Form
    {
        internal string pass = "";

        public Password()
        {
            InitializeComponent();
        }

        private void MetroButton2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void MetroButton1_Click(object sender, EventArgs e)
        {
            pass = textBox1.Text;
            this.Close();
        }

        private void Password_Load(object sender, EventArgs e)
        {
            this.textBox1.KeyPress += new KeyPressEventHandler(CheckEnterKeyPress);
        }

        private void CheckEnterKeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)

            {
                MetroButton1_Click(sender, e);
            }
        }
    }
}
