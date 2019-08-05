//============================================================================//
//                              LOAD PROJECT                                  //
//                                                                            //
// - Load project from distant repositories or from URL                       //
//============================================================================//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace module_manager
{
    public partial class LoadProjForm : Form
    {
        internal string path = "";
        List<Repo> repoList = new List<Repo>();
        Functions functions;
        internal string id = "";

        public LoadProjForm()
        {
            InitializeComponent();
            functions = new Functions();
            Icon = Icon.ExtractAssociatedIcon("logo.ico");
        }

        private void Form5_Load(object sender, EventArgs e)
        {
            repoList = MainForm.repoList.ToList();
            repoList = repoList.OrderBy(repo => repo.Name).ToList();
            foreach (Repo rep in repoList)
            {
                if(!rep.Name.Contains("MODULES"))
                    dataGridView1.Rows.Add(rep.Name, rep.ServerName ?? rep.Server, rep.Id.ToString());
            }
        }

        private void MetroButton1_Click(object sender, EventArgs e)
        {
            if(textBox1.Text.Length !=0)
            {
                path = textBox1.Text;
                this.Close();
            }
        }

        private void DataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 0)
            {
                id = dataGridView1[e.ColumnIndex + 2, e.RowIndex].Value.ToString();
                this.Close();
            }
        }
    }
}
