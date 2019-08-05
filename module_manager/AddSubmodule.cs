//============================================================================//
//                              ADD SUBMODULE                                 //
//                                                                            //
// - Load module list                                                         //
// - Check #includes for selected modules                                     //
// - Clone submodules                                                         //
//============================================================================//

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
    public partial class AddSubmodule : Form
    {
        List<Repo> moduleList = new List<Repo>();
        List<Repo> checkedList = new List<Repo>();
        List<Repo> repoList = new List<Repo>();
        Repo rep;
        string selectedPath = "";
        Functions functions;
        Config config;

        public AddSubmodule(Repo repository)
        {
            InitializeComponent();
            rep = repository;
            selectedPath = MainForm.selectedPath;
            Icon = Icon.ExtractAssociatedIcon("logo.ico");
        }

        private void AddSubmodule_Load(object sender, EventArgs e)
        {
            treeView1.Visible = false;
            metroButton2.Visible = false;
            config = new Config();
            try { functions = MainForm.functions; }
            catch (Exception) { functions = new Functions(); }

            try { repoList = MainForm.repoList.ToList(); }
            catch (Exception)
            {
                Console.WriteLine("Can't retreive repoList from MainForm");
                repoList = functions.GetRepoList();
            }

            treeView1.DrawMode = TreeViewDrawMode.OwnerDrawText;
            treeView1.DrawNode += new DrawTreeNodeEventHandler(TreeView1_DrawNode);
            backgroundWorker1.RunWorkerAsync();
        }


        private void CloseForm(object sender, FormClosedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Charge les modules dans le DataGridView et désactive ceux qui appartiennent déjà au projet
        /// </summary>
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

        /// <summary>
        /// Bouton "Suivant" : soit lance le bgWorker2, soit lance le bgWorker3 suivant le control visible
        /// </summary>
        private void MetroButton1_Click(object sender, EventArgs e)
        {
            if(!treeView1.Visible)
            {
                toolStripProgressBar1.Visible = true;
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

                if (idList.Count() == 0)
                {
                    MessageBox.Show("Veuillez sélectionner un module", "Aucun module séléctionné", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                backgroundWorker2.RunWorkerAsync(argument: idList);
            }
            else
            {
                toolStripProgressBar1.Visible = true;
                foreach (TreeNode treeNode in treeView1.Nodes)
                {
                    if (treeNode.Checked == true)
                    {
                        checkedList.Add((Repo)treeNode.Tag);
                    }
                }
                backgroundWorker3.RunWorkerAsync();
            }
        }

        private void MetroButton2_Click(object sender, EventArgs e)
        {
            if(treeView1.Visible)
            {
                treeView1.Visible = false;
                panel2.Visible = true;
                metroButton2.Visible = false;
            }
        }

        private void MetroButton3_Click(object sender, EventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Ajoute les modules séléctionnés au TreeView et affiche les #includes
        /// </summary>
        private void BackgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            List<string> idList = (List<string>)e.Argument;
            int i = 0;
            int counter = idList.Count();
            
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
                    List<string> dependencies = functions.GetModuleDep(repo, config.GetBranchDev());
                    foreach (string dep in dependencies)
                    {
                        treeNode.Nodes.Add(new TreeNode(dep));
                    }
                    treeView1.Invoke(new Action(() => treeView1.Nodes.Add(treeNode)));
                    i++;
                    worker.ReportProgress(i * 100 / counter);
                }
            }
        }

        private void BackgroundWorker2_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            toolStripProgressBar1.Value = e.ProgressPercentage;
        }

        private void BackgroundWorker2_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            toolStripProgressBar1.Value = 0;
            toolStripProgressBar1.Visible = false;
            panel2.Visible = false;
            treeView1.Visible = true;
            metroButton2.Visible = true;
        }

        private void TreeView1_DrawNode(object sender, DrawTreeNodeEventArgs e)
        {
            if (e.Node.Level == 1) e.Node.HideCheckBox();
            e.DrawDefault = true;
        }

        /// <summary>
        /// Clone les modules sélectionnés
        /// </summary>
        private void BackgroundWorker3_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            int counter = 1;
            int size = checkedList.Count();
            foreach (Repo repo in checkedList)
            {
                try
                {
                    Process process = new Process();
                    process.StartInfo.FileName = config.GetAppData() + @"clone.bat";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    process.StartInfo.WorkingDirectory = selectedPath;
                    string currentType = repo.Server;
                    
                    if (currentType == "devops")
                    {
                        process.StartInfo.CreateNoWindow = false;
                        process.StartInfo.Arguments = repo.Url + " " + "MODULES/" + repo.Name;
                    }
                    else
                        process.StartInfo.Arguments = repo.Url + " " + "MODULES/" + repo.Name;

                    process.Start();
                    e.Result = process.StandardError.ReadToEnd(); // Récupère les erreurs et warning du process
                    while (!process.StandardOutput.EndOfStream)
                    {
                        string line = process.StandardOutput.ReadLine();
                        if (line == "\"status 25\"")
                        {
                            worker.ReportProgress(counter);
                            counter += (100 / size) / 4;
                        }
                        else if (line == "\"status 50\"")
                        {
                            worker.ReportProgress(counter);
                            counter += (100 / size) / 4;
                        }
                        else if (line == "\"status 75\"")
                        {
                            worker.ReportProgress(counter);
                            counter += (100 / size) / 4;
                        }
                        else if (line == "\"status 100\"")
                        {
                            worker.ReportProgress(counter);
                            counter += (100 / size) / 4;
                        }
                        if (backgroundWorker1.CancellationPending)
                        {
                            e.Cancel = true;
                            return;
                        }
                    }
                    process.WaitForExit();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private void BackgroundWorker3_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            toolStripProgressBar1.Value = e.ProgressPercentage;
        }

        private void BackgroundWorker3_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Result.ToString().Contains("fatal"))
                MessageBox.Show(e.Result.ToString(), "Attention", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Close();
        }

        private void DataGridView1_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            if (dataGridView1.Rows[e.RowIndex].Cells[1].Value.ToString().Contains("(✓)"))
            {
                DataGridViewCell cell = dataGridView1.Rows[e.RowIndex].Cells[0];
                DataGridViewCheckBoxCell chkCell = cell as DataGridViewCheckBoxCell;
                chkCell.FlatStyle = FlatStyle.Flat;
                chkCell.Style.ForeColor = Color.DarkGray;
                cell.ReadOnly = true;

            }
        }
    }
}
