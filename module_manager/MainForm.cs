using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;

namespace module_manager
{
    public partial class MainForm : Form
    {
        public static List<string> repoList;
        public static List<List<string>> projList;
        private List<string> smartList;
        Functions functions;
        bool bg3IsWorking = false;

        public MainForm()
        {
            InitializeComponent();
            functions = new Functions();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            metroTabControl2.SelectedTab = metroTabPage3;
            LoadForm();
        }

        private void LoadForm()
        {
            repoList = new List<string>();
            projList = new List<List<string>>();
            smartList = new List<string>();
            toolStripStatusLabel2.Text = "";
            metroLabel6.Text = "";
            treeView2.Nodes.Clear();
            treeView2.Enabled = false;
            treeView1.Nodes.Clear();
            treeView1.Enabled = false;
            metroTabControl1.Enabled = false;
            metroButton1.Enabled = false;
            treeView1.Nodes.Clear();
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
                if(rep.Contains("MODULE"))
                    treeView2.Invoke(new Action(() => treeView2.Nodes.Add(new TreeNode(rep.Replace(".git","")))));
                // else {
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
                // }
                
            }
                        
        }

        private void TreeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            metroLabel5.Text = e.Node.Text;
            
            if(e.Node.Name != "module")
            {
                metroButton1.Enabled = true;
                toolStripStatusLabel2.Text = e.Node.Name;
            } else
            {
                metroButton1.Enabled = false;
                toolStripStatusLabel2.Text = e.Node.Parent.Name;
            }
            int item = 0;
            foreach(string rep in repoList)
            {
                if(rep.Contains(e.Node.Text))
                {
                    metroLabel6.Text = Functions.descList.ElementAt(item);
                    if(!bg3IsWorking)
                        backgroundWorker3.RunWorkerAsync(argument: rep.Replace(".git", ""));
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
                            dataGridView1.Rows.Add(repoList.ElementAt(i), "", "Détails");
                        }
                    }
                    i++;
                }
            }
            else
            {
                List<string> localModules = functions.SearchGitmodulesFile(@toolStripStatusLabel2.Text);
                List<string> distantModules = new List<string>();
                metroLabel4.Text = "Modules présents dans le projet";
                int i = 0;
                foreach(List<string> proj in projList)
                {
                    if(repoList.ElementAt(i).Contains(e.Node.Text))
                    {
                        foreach(string module in proj)
                        {
                            if(localModules.Contains(module))
                            {
                                dataGridView1.Rows.Add(module.Substring(module.IndexOf(@"/r/") + 3, module.Length - module.IndexOf(@"/r/") - 3), "Distant / Local", "Supprimer");
                            }
                            else
                            {
                                dataGridView1.Rows.Add(module.Substring(module.IndexOf(@"/r/") + 3, module.Length - module.IndexOf(@"/r/") - 3), "Distant", "Dépendances");
                            }
                            distantModules.Add(module);
                        }
                        break;
                    }
                    
                    i++;
                }

                foreach (string module in localModules)
                {
                    if(!distantModules.Contains(module))
                        dataGridView1.Rows.Add(module.Substring(module.IndexOf(@"/r/") + 3, module.Length - module.IndexOf(@"/r/") - 3), "Local", "Supprimer");
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
            treeView2.Enabled = true;
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

            if (senderGrid.Columns[e.ColumnIndex] is DataGridViewButtonColumn && e.RowIndex >= 0)
            {
                if((string) senderGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value == "Supprimer")
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
                else if ((string)senderGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value == "Détails")
                {
                    treeView1.SelectedNode = null;
                    string projName = dataGridView1.Rows[e.RowIndex].Cells[0].Value.ToString().Replace(".git", "");
                    metroLabel5.Text = projName;
                    toolStripStatusLabel2.Text = "";
                    int item = 0;
                    foreach (string rep in repoList)
                    {
                        if (rep.Contains(projName))
                        {
                            metroLabel6.Text = Functions.descList.ElementAt(item);
                            if (!bg3IsWorking)
                                backgroundWorker3.RunWorkerAsync(argument: rep.Replace(".git", ""));
                            break;
                        }
                        item++;
                    }
                    dataGridView1.Rows.Clear();
                    dataGridView1.Refresh();
                    metroLabel4.Text = "Modules présents dans le projet";
                    int i = 0;
                    foreach (List<string> proj in projList)
                    {
                        if (repoList.ElementAt(i).Contains(projName))
                        {
                            foreach (string module in proj)
                            {
                                dataGridView1.Rows.Add(module.Substring(module.IndexOf(@"/r/") + 3, module.Length - module.IndexOf(@"/r/") - 3), "Distant", "Dépendances");
                            }
                            break;
                        }
                        i++;
                    }
                }
                else if ((string)senderGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value == "Dépendances")
                {
                    string modName = dataGridView1.Rows[e.RowIndex].Cells[0].Value.ToString();
                    metroLabel5.Text = modName;
                    metroButton1.Enabled = false;
                    toolStripStatusLabel2.Text = "";
                    int item = 0;
                    foreach (string rep in repoList)
                    {
                        if (rep.Contains(modName))
                        {
                            metroLabel6.Text = Functions.descList.ElementAt(item);
                            if (!bg3IsWorking)
                                backgroundWorker3.RunWorkerAsync(argument: rep.Replace(".git", ""));
                            break;
                        }
                        item++;
                    }
                    dataGridView1.Rows.Clear();
                    dataGridView1.Refresh();
                    metroLabel4.Text = "Projets dépendants du module";
                    int i = 0;
                    foreach (List<string> proj in projList)
                    {
                        foreach (string module in proj)
                        {
                            if (module.Replace(".git", "").Contains(modName.Replace(".git", "")))
                            {
                                dataGridView1.Rows.Add(repoList.ElementAt(i), "", "Détails");
                            }
                        }
                        i++;
                    }
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
            var frm = new AddSubForm(arg);
            frm.Location = this.Location;
            frm.StartPosition = FormStartPosition.Manual;
            frm.FormClosed += AddModuleFormClosed;
            frm.Show();
        }

        private void AddModuleFormClosed(object sender, FormClosedEventArgs e)
        {
            TreeNode selectedNode = treeView1.SelectedNode;
            treeView1.SelectedNode = null;
            treeView1.SelectedNode = selectedNode;
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
            TreeNode selectedNode = treeView1.SelectedNode;
            treeView1.SelectedNode = null;
            treeView1.SelectedNode = selectedNode;
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

        private void MetroButton2_Click(object sender, EventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            DialogResult result = dialog.ShowDialog();
            string path = dialog.SelectedPath;
            if (path.Length != 0)
            {
                var directories = Directory.GetDirectories(path, ".git");
                if (directories.Length == 0) // Si le dossier n'est pas un repo git
                {
                    MessageBox.Show("Le répertoire sélectionné n'est pas un dépôt Git", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    TreeNode treeNode = new TreeNode(path.Substring(path.LastIndexOf("\\") + 1, path.Length - path.LastIndexOf("\\") - 1));
                    treeNode.Name = path;
                    treeView1.Nodes.Add(treeNode);
                    smartList.Add(path);

                    List<string> subrepo = functions.SearchGitmodulesFile(path); // Liste contenant les subrepo = modules
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

        private void ToolStripStatusLabel2_Click(object sender, EventArgs e)
        {
            if(toolStripStatusLabel2.Text != "")
                Process.Start(@toolStripStatusLabel2.Text);
        }

        private void MetroTabPage4_Click(object sender, EventArgs e)
        {

        }

        private void TreeView2_AfterSelect(object sender, TreeViewEventArgs e)
        {
            metroLabel5.Text = e.Node.Text;
            metroButton1.Enabled = false;
            toolStripStatusLabel2.Text = "";
            int item = 0;
            foreach (string rep in repoList)
            {
                if (rep.Contains(e.Node.Text))
                {
                    metroLabel6.Text = Functions.descList.ElementAt(item);
                    if (!bg3IsWorking)
                        backgroundWorker3.RunWorkerAsync(argument: rep.Replace(".git", ""));
                    break;
                }
                item++;
            }
            dataGridView1.Rows.Clear();
            dataGridView1.Refresh();
            metroLabel4.Text = "Projets dépendants du module";
            int i = 0;
            foreach (List<string> proj in projList)
            {
                foreach (string module in proj)
                {
                    if (module.Contains(e.Node.Text))
                    {
                        dataGridView1.Rows.Add(repoList.ElementAt(i), "", "Détails");
                    }
                }
                i++;
            }
        }

        private void MetroLabel7_Click(object sender, EventArgs e)
        {
            using (LoadProjForm formOptions = new LoadProjForm())
            {
                formOptions.ShowDialog();
                try
                {
                    string result = formOptions.path;
                    if(result.Length != 0)
                        treeView1.Nodes.Add(new TreeNode(result));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private void DepuisUnDépôtLocalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            DialogResult result = dialog.ShowDialog();
            string path = dialog.SelectedPath;
            if (path.Length != 0)
            {
                var directories = Directory.GetDirectories(path, ".git");
                if (directories.Length == 0) // Si le dossier n'est pas un repo git
                {
                    MessageBox.Show("Le répertoire sélectionné n'est pas un dépôt Git", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    TreeNode treeNode = new TreeNode(path.Substring(path.LastIndexOf("\\") + 1, path.Length - path.LastIndexOf("\\") - 1));
                    treeNode.Name = path;
                    treeView1.Nodes.Add(treeNode);
                    smartList.Add(path);

                    List<string> subrepo = functions.SearchGitmodulesFile(path); // Liste contenant les subrepo = modules
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

        private void DepuisUnURLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (LoadProjForm formOptions = new LoadProjForm())
            {
                formOptions.ShowDialog();
                try
                {
                    string result = formOptions.path;
                    if (result.Length != 0)
                        treeView1.Nodes.Add(new TreeNode(result));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private void BackgroundWorker3_DoWork(object sender, DoWorkEventArgs e)
        {
            bg3IsWorking = true;
            string project = (string) e.Argument;
            string md = functions.GetMarkdown(project, "_DEV_");
            string html = "";
            try
            {
                html = Markdig.Markdown.ToHtml(md);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Markdow ToHTML error : " + ex.Message);
            }
            html = html.Replace("img src=\"", "img src=\"http://192.168.55.218:8082/raw/" + project + ".git/master/");
            html = html.Replace(@"%5C", @"/");
            e.Result = html;
        }

        private void BackgroundWorker3_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            webBrowser1.Navigate("about:blank");
            try
            {
                if (webBrowser1.Document != null)
                {
                    webBrowser1.Document.Write(string.Empty);
                }
                webBrowser1.DocumentText = (string)e.Result.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Web brower error : " + ex.Message);
            }
            bg3IsWorking = false;
        }

        private void QuitterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void GérerLesSourcesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Console.WriteLine(functions.GetRepoListBitBucket());
        }

        private void ComptesEtConnexionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            byte[] entropy = File.ReadAllBytes(@"./.creditEnt"); //TODO: Change to global password
            byte[] ciphertext = File.ReadAllBytes(@"./.creditCip");
            byte[] returntext = ProtectedData.Unprotect(ciphertext, entropy, DataProtectionScope.CurrentUser);
            string result = Encoding.UTF8.GetString(returntext);

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            string url = "https://api.bitbucket.org/2.0/repositories/bglx/projet_002/src/master/.gitmodules";
            WebRequest myReq = WebRequest.Create(url);
            myReq.Method = "GET";
            string credentials = "bglx:" + result;
            CredentialCache mycache = new CredentialCache();
            myReq.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(credentials));
            WebResponse wr = myReq.GetResponse();
            Stream receiveStream = wr.GetResponseStream();
            StreamReader reader = new StreamReader(receiveStream, Encoding.UTF8);
            Console.WriteLine(reader.ReadToEnd());
        }
    }
    
}
