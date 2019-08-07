//============================================================================//
//                              PASSWORD FORM                                 //
//                                                                            //
// - Display a form to enter a password                                       //
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
    public partial class Password : Form
    {
        internal string pass = "";

        public Password()
        {
            InitializeComponent();
            Icon = Icon.ExtractAssociatedIcon("logo.ico");
        }

        private void MetroButton2_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void MetroButton1_Click(object sender, EventArgs e)
        {
            pass = textBox1.Text;
            Close();
        }

        private void Password_Load(object sender, EventArgs e)
        {
            textBox1.KeyPress += new KeyPressEventHandler(CheckEnterKeyPress);
        }

        private void CheckEnterKeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)
                MetroButton1_Click(sender, e);
        }
    }
}
