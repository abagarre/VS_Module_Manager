using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace module_manager
{
    public partial class LoadProjForm : Form
    {
        internal string path = "";
        List<string> repoList = new List<string>();
        Functions functions;

        public LoadProjForm()
        {
            InitializeComponent();
            functions = new Functions();
        }

        private void Form5_Load(object sender, EventArgs e)
        {
            repoList = MainForm.repoList.ToList();
            repoList.Sort();
            foreach(string rep in repoList)
            {
                if(!rep.Contains("MODULES"))
                    dataGridView1.Rows.Add(rep);
            }
        }

        private void MetroButton1_Click(object sender, EventArgs e)
        {
            if(textBox1.Text.Length !=0)
            {
                path = textBox1.Text;
                path = path.Replace(".git", "");
                //============================== PATH DELIMITER ===========================//
                path = path.Substring(path.IndexOf(@"_"), path.Length - path.IndexOf(@"_"));
                //=========================================================================//
                this.Close();
            }
        }

        private void Label1_Click(object sender, EventArgs e)
        {

        }

        private void DataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if(e.RowIndex >= 0)
            {
                path = dataGridView1[e.ColumnIndex, e.RowIndex].Value.ToString();
                path = path.Replace(".git", "");
                this.Close();
            }
        }
    }
}
