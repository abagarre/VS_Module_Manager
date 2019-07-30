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
    public partial class AddSubmodule : Form
    {
        List<Repo> moduleList = new List<Repo>();
        List<Repo> repoList = new List<Repo>();
        Repo rep;
        Functions functions;
        public AddSubmodule(Repo repository)
        {
            InitializeComponent();
            rep = repository;
        }

        private void AddSubmodule_Load(object sender, EventArgs e)
        {
            try { functions = MainForm.functions; }
            catch (Exception) { functions = new Functions(); }

            try { repoList = MainForm.repoList.ToList(); }
            catch (Exception)
            {
                Console.WriteLine("Can't retreive repoList from MainForm");
                repoList = functions.GetRepoList();
            }

            backgroundWorker1.RunWorkerAsync();
        }


        private void CloseForm(object sender, FormClosedEventArgs e)
        {
            this.Close();
        }


        private void BackgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            int i = 0, j = 0;

            List<Repo> modList = repoList.OrderBy(mod => mod.Name).ToList();
            i = 0;
            foreach (Repo mod in modList)
            {
                if (mod.Type == "module")
                {
                    int toAdd = 1;

                    if (mod.IsInList(rep.Modules))
                    {
                        // Si le module est déjà ajouté au projet, ajoute un indicateur et grise la case
                        dataGridView1.Invoke(new Action(() => dataGridView1.Rows.Add(true, mod.Name + " (✓)", mod.ServerName, mod.Id)));
                        dataGridView1.Invoke(new Action(() => dataGridView1.Rows[i].Cells[0].ReadOnly = true));
                        toAdd = 0;
                    }

                    if (toAdd == 1)
                    {
                        dataGridView1.Invoke(new Action(() => dataGridView1.Rows.Add(false, mod.Name, mod.ServerName, mod.Id)));
                    }

                    i++;

                }
                j++;
                worker.ReportProgress(j * 100 / modList.Count());

            }
            worker.ReportProgress(0);
        }

        private void BackgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            toolStripProgressBar1.Value = e.ProgressPercentage;
        }

        private void BackgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            toolStripStatusLabel1.Text = "Prêt";
            toolStripProgressBar1.Visible = false;
        }

        private void MetroButton1_Click(object sender, EventArgs e)
        {
            treeView1.Nodes.Clear();
            List<string> idList = new List<string>();
            var checkedRows = from DataGridViewRow r in dataGridView1.Rows
                              where Convert.ToBoolean(r.Cells[0].Value) == true
                              select r;

            foreach (var row in checkedRows)
            {
                if (!row.Cells[1].Value.ToString().Contains("(✓)"))
                    idList.Add(row.Cells[3].Value.ToString());
            }

            if (idList.Count() != 0)
            {
                foreach (Repo repo in repoList)
                {
                    if (idList.Contains(repo.Id.ToString()))
                    {
                        moduleList.Add(repo);
                        TreeNode treeNode = new TreeNode(repo.Name)
                        {
                            Tag = repo,
                            Checked = true
                        };
                        treeView1.Nodes.Add(treeNode);
                    }
                }
            }
        }

        private void MetroButton2_Click(object sender, EventArgs e)
        {

        }

        private void MetroButton3_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
