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
using System.Xml.Linq;

namespace module_manager
{
    public partial class Form1 : Form
    {
        public static List<string> repoList;
        public static List<List<string>> projList;
        private List<string> smartList;
        Functions functions;

        public Form1()
        {
            InitializeComponent();
            functions = new Functions();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadForm();
        }

        private void LoadForm()
        {
            repoList = new List<string>();
            projList = new List<List<string>>();
            smartList = new List<string>();
            treeView1.Nodes.Clear();
            treeView1.Enabled = false;
            metroTabControl1.Enabled = false;
            dataGridView1.Rows.Clear();
            dataGridView1.Refresh();
            metroLabel5.Text = "";
            toolStripStatusLabel1.Text = "Chargement...";
            toolStripProgressBar1.Visible = true;
            toolStripProgressBar1.Value = 0;
            toolStripSplitButton1.Visible = true;
            toolStripSplitButton2.Visible = false;
            backgroundWorker1.RunWorkerAsync();
            this.metroTextBox1.KeyPress += new KeyPressEventHandler(CheckEnterKeyPress);
            metroTabPage1.Text = "Informations";
            metroTabPage2.Text = "Dépendances";

            try
            {
                XDocument xml = XDocument.Load(@"C:\Users\STBE\Downloads\SmartGit\.settings\repositories.xml");

                IEnumerable<XElement> ob = xml.Root.Elements();
                IEnumerable<XElement> coll = ob.ElementAt(0).Elements();

                for (int i = 0; i < coll.Count(); i++)
                {
                    XElement obj = coll.ElementAt(i);
                    var query = from c in obj.Descendants("prop")
                                where c.Attribute("key").Value == "path"
                                select new
                                {
                                    path = c.Attribute("value").Value
                                };
                    foreach (var path in query)
                    {
                        TreeNode treeNode = new TreeNode(path.path.Substring(path.path.LastIndexOf("\\") + 1, path.path.Length - path.path.LastIndexOf("\\") - 1));
                        treeNode.Name = path.path;
                        treeView1.Nodes.Add(treeNode);
                        smartList.Add(path.path);

                        List<string> subrepo = functions.SearchGitmodulesFile(path.path); // Liste contenant les subrepo = modules
                        foreach (string rep in subrepo) // Pour chaque module, créé un bouton redirigeant vers la page du module
                        {
                            string name = rep.Substring(rep.LastIndexOf("/") + 1, rep.Length - rep.LastIndexOf("/") - 1);
                            TreeNode childNode = new TreeNode(name);
                            childNode.Name = "module";
                            treeNode.Nodes.Add(childNode);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void BackgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            // Récupère la liste des projets dépendants du module en parcourant tous les prjets et en lisant le fichier .gitmodule
            repoList = functions.DispRepoList();
            int i = 1;
            foreach(string rep in repoList)
            {
                try
                {
                    List<string> proj = functions.GetModList("_DEV_", rep);
                    projList.Add(proj);
                }
                catch (Exception ex)
                {
                    projList.Add(new List<string>());
                    Console.WriteLine(ex.Message);
                }
                worker.ReportProgress((i * 100) / repoList.Count);
                i++;
                if (backgroundWorker1.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }
            }
                        
        }

        private void TreeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            
            metroLabel3.Text = e.Node.Text;
            metroLabel5.Text = e.Node.Text;
            if(e.Node.Name != "module")
            {
                toolStripStatusLabel2.Text = e.Node.Name;
            } else
            {
                toolStripStatusLabel2.Text = e.Node.Parent.Name;
            }
            int item = 0;
            foreach(string rep in repoList)
            {
                if(rep.Contains(e.Node.Text))
                {
                    metroLabel6.Text = Functions.descList.ElementAt(item);
                    break;
                }
                item++;
            }
            dataGridView1.Rows.Clear();
            dataGridView1.Refresh();
            if (e.Node.Name == "module")
            {
                metroLabel4.Text = "Projets dépendants du module";
                int i = 0;
                foreach(List<string> proj in projList)
                {
                    foreach(string module in proj)
                    {
                        if(module.Contains(e.Node.Text))
                        {
                            dataGridView1.Rows.Add(repoList.ElementAt(i),i,"Détails");
                        }
                    }
                    i++;
                }
                
            } else
            {
                metroLabel4.Text = "Modules présents dans le projet";
                int i = 0;
                foreach(List<string> proj in projList)
                {
                    if(repoList.ElementAt(i).Contains(e.Node.Text))
                    {
                        foreach(string module in proj)
                        {
                            dataGridView1.Rows.Add(module.Substring(module.IndexOf(@"/r/") + 4, module.Length - module.IndexOf(@"/r/") - 4), i, "Supprimer");
                        }
                        break;
                    }
                    
                    i++;
                }

                var match = repoList.FirstOrDefault(stringToCheck => stringToCheck.Contains(e.Node.Text));
                if (match == null)
                {
                    foreach (string module in functions.SearchGitmodulesFile(@toolStripStatusLabel2.Text))
                    {
                        dataGridView1.Rows.Add(module.Substring(module.IndexOf(@"/r/") + 3, module.Length - module.IndexOf(@"/r/") - 3), i, "Supprimer");
                    }
                }
            }
        }

        

        private void StatusStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void BackgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            toolStripStatusLabel1.Text = "Prêt";
            toolStripProgressBar1.Visible = false;
            toolStripSplitButton1.Visible = false;
            metroTabControl1.Enabled = true;
            treeView1.Enabled = true;
        }

        private void BackgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            toolStripProgressBar1.Value = e.ProgressPercentage;
        }

        private void MetroTabPage1_Click(object sender, EventArgs e)
        {

        }

        private void DataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            var senderGrid = (DataGridView)sender;

            if (senderGrid.Columns[e.ColumnIndex] is DataGridViewButtonColumn &&
                e.RowIndex >= 0)
            {
                string modName = dataGridView1.Rows[e.RowIndex].Cells[0].Value.ToString();
                if (MessageBox.Show("Voulez vous supprimer le module " + modName + " du projet " + metroLabel5.Text + " ?", "Supprimer un module", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    toolStripProgressBar1.Visible = true;
                    toolStripSplitButton2.Visible = true;
                    toolStripProgressBar1.Value = 0;
                    toolStripStatusLabel1.Text = "Suppression...";
                    metroTabControl1.Enabled = false;
                    backgroundWorker2.RunWorkerAsync(argument: modName);
                }
            }
        }

        private void MetroTextBox1_Click(object sender, EventArgs e)
        {
            //metroTextBox1.Text = "";
        }

        private void MetroTextBox1_TextChanged(object sender, EventArgs e)
        {
            
        }

        private void CheckEnterKeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)
            {
                BindingSource bs = new BindingSource();
                bs.DataSource = dataGridView1.DataSource as DataTable;
                bs.Filter = "Nom like '%" + metroTextBox1.Text + "%'";
                dataGridView1.DataSource = bs;

            }
        }

        private void MetroButton1_Click(object sender, EventArgs e)
        {
            string[] arg = { toolStripStatusLabel2.Text };
            var frm = new Form2(arg);
            frm.Location = this.Location;
            frm.StartPosition = FormStartPosition.Manual;
            frm.Show();
        }

        private void ToolStripDropDownButton1_Click(object sender, EventArgs e)
        {
            
        }

        private void ToolStripSplitButton1_ButtonClick(object sender, EventArgs e)
        {
            backgroundWorker1.CancelAsync();
        }

        private void BackgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            Process process = new Process();
            process.StartInfo.FileName = @"C:\Users\STBE\Downloads\PortableGit\home\TestMaster\del_sub.bat";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.WorkingDirectory = @toolStripStatusLabel2.Text;
            process.StartInfo.Arguments = (string) e.Argument;
            process.Start();
            e.Result = process.StandardError.ReadToEnd();
            while (!process.StandardOutput.EndOfStream)
            {
                string line = process.StandardOutput.ReadLine();
                Console.WriteLine(line);
                if (line == "\"status 25\"")
                {
                    worker.ReportProgress(25);
                }
                else if (line == "\"status 50\"")
                {
                    worker.ReportProgress(50);
                }
                else if (line == "\"status 75\"")
                {
                    worker.ReportProgress(75);
                }
                else if (line == "\"status 100\"")
                {
                    worker.ReportProgress(100);
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
            toolStripSplitButton2.Visible = false;
            toolStripStatusLabel1.Text = "Prêt";
            metroTabControl1.Enabled = true;
            if(e.Result.ToString().Length != 0)
                MessageBox.Show(e.Result.ToString(), "Message", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void ToolStripSplitButton2_ButtonClick(object sender, EventArgs e)
        {
            backgroundWorker2.CancelAsync();
        }

        private void ToolStripSplitButton3_ButtonClick(object sender, EventArgs e)
        {
            LoadForm();
        }
    }
    
}
