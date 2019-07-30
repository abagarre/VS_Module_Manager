using Octokit;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace module_manager
{
    public partial class MainForm : Form
    {
        public static List<Repo> repoList;        // Liste des dépots distants
        public static List<List<string>> projList;  // Liste des listes des sous-modules de chaque dépôt de repoList
        private List<string> clientList;            // Liste des projets ouverts dans SmartGit
        private List<int> readmeState;
        public static Functions functions;
        Config config;
        bool bg3IsWorking = false;
        List<Repo> repositories = new List<Repo>();
        List<Repo> modules = new List<Repo>();

        public MainForm()
        {
            InitializeComponent();
            menuStrip1.Renderer = new ToolStripProfessionalRenderer(new CustomProfessionalColors());
            treeView1.NodeMouseClick += (sender, args) => treeView1.SelectedNode = args.Node;
            functions = new Functions();
            config = new Config();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            metroTabControl2.SelectedTab = metroTabPage3;   // Focus sur les projets ouverts
            LoadForm();
        }

        ///<summary>
        ///Remise à zéro de toute les listes, Labels, TreeViews, DataGridViews...
        ///</summary>
        private void LoadForm()
        {
            repoList = new List<Repo>();
            projList = new List<List<string>>();
            readmeState = new List<int>();
            repositories = new List<Repo>();
            toolStripStatusLabel2.Text = "";
            label2.Text = "";
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
            toolStripStatusLabel3.Text = config.GetCurrentSource();
            backgroundWorker1.RunWorkerAsync();
            metroTextBox1.KeyPress += new KeyPressEventHandler(CheckEnterKeyPress);
            comboBox1.SelectedValue = "Local";
            webBrowser1.Navigate("about:blank");
            ContextMenuStrip contextMenuStrip;
            try
            {
                if(config.GetClient() == "smartgit")
                {
                    clientList = functions.GetSmartGitList();
                }
                else if(config.GetClient() == "sourcetree")
                {
                    clientList = functions.GetSourceTreetList();
                }
                else if(config.GetClient() == "dossierlocal")
                {
                    clientList = functions.GetLocalList(config.GetLocalRepo());
                }
                foreach (string chemin in clientList)
                {
                    Repo repo = new Repo();
                    repo = repo.Init(chemin);
                    
                    string name = chemin.Substring(chemin.LastIndexOf("\\") + 1, chemin.Length - chemin.LastIndexOf("\\") - 1).Replace(".git", "");
                    TreeNode treeNode = new TreeNode(name);

                    if(repo.Type == "project")
                    {
                        List<Repo> gitmodulesLocList = functions.GetGitmodulesLoc(chemin); // Liste contenant les modules
                        int j = 0;
                        foreach (Repo submodule in gitmodulesLocList) // Ajoute chaque module en tant que fils dans l'arborescence des projets
                        {
                            TreeNode childNode = new TreeNode(submodule.Name);
                            childNode.Tag = submodule;

                            contextMenuStrip = new ContextMenuStrip();
                            contextMenuStrip.Items.Add("Ouvrir (local)");
                            contextMenuStrip.Items.Add("Ouvrir (URL)");
                            contextMenuStrip.Items.Add("Déplacer");
                            contextMenuStrip.Items.Add("Supprimer");
                            contextMenuStrip.ItemClicked += ContextMenuStripClick;
                            contextMenuStrip.Text = childNode.Text;
                            contextMenuStrip.Name = treeNode.Name + @"\" + childNode.Tag.ToString();
                            contextMenuStrip.Tag = submodule;
                            childNode.ContextMenuStrip = contextMenuStrip;

                            treeNode.Nodes.Add(childNode);
                            repo.Modules.Add(submodule);
                            if(!submodule.IsInList(modules))
                                modules.Add(submodule);
                            repositories.Add(submodule);
                            j++;
                        }
                    }
                    
                    contextMenuStrip = new ContextMenuStrip();
                    contextMenuStrip.Items.Add("Ouvrir (local)");
                    contextMenuStrip.Items.Add("Ouvrir (URL)");
                    contextMenuStrip.ItemClicked += ContextMenuStripClick;
                    contextMenuStrip.Name = treeNode.Name;
                    contextMenuStrip.Tag = repo;
                    treeNode.ContextMenuStrip = contextMenuStrip;
                    treeNode.Tag = repo;
                    treeView1.Nodes.Add(treeNode);  // Ajout du projet au TreeView
                    if(!repo.IsInList(repositories))
                        repositories.Add(repo);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        ///<summary>
        ///Récupère toutes les informations des projet locaux et des modules distants
        ///</summary>
        private void BackgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            string currentServ = config.GetCurrentSource();

            List<string> allNames = config.GetAllNames();
            int servNb = allNames.Count();
            int j = 0;
            foreach (string name in allNames)
            {
                config.ChangeServer(name);
                List<Repo> repos = functions.GetRepoList();
                int i = 0;
                foreach (Repo repo in repos) // Récupère la liste de tous les dépôts distants
                {
                    repo.Modules = new List<Repo>();

                    // Récupère la liste des modules de ce projet et l'ajoute à la liste projList
                    try
                    {
                        List<Repo> proj = functions.GetSubmodList(config.GetBranchDev(), repo.Name, repo);
                        repo.Modules = proj;
                    }
                    catch (Exception)
                    {
                        // Si pas de modules, ajoute liste vide
                        repo.Modules = new List<Repo>();
                    }
                    if (repo.Modules.Count() == 0)
                    {
                        repo.Type = "module";
                        treeView2.Invoke(new Action(() => treeView2.Nodes.Add(new TreeNode(repo.Name) { Tag = repo }))) ;
                    }
                    else
                    {
                        repo.Type = "project";
                    }
                    repoList.Add(repo);
                    worker.ReportProgress(i * 100 / (servNb * repos.Count()) + j * 100 / servNb);
                    i++;
                    if (backgroundWorker1.CancellationPending)
                    {
                        e.Cancel = true;
                        return;
                    }
                }
                j++;
            }
            config.ChangeServer(currentServ);
        }

        private void BackgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            toolStripStatusLabel1.Text = "Prêt";
            toolStripProgressBar1.Visible = false;
            toolStripSplitButton1.Visible = false;
            metroTabControl1.Enabled = true;
            treeView1.Enabled = true;
            treeView2.Enabled = true;
            Activate();
        }

        private void BackgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            toolStripProgressBar1.Value = e.ProgressPercentage;
        }
        private void ToolStripSplitButton1_ButtonClick(object sender, EventArgs e)
        {
            backgroundWorker1.CancelAsync();
        }

        ///<summary>
        ///Au clic sur un noeud du TreeView1 (projets) :<para />
        /// - Affiche le README<para />
        /// - Charge les dépendances
        ///</summary>
        private void TreeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            metroLabel5.Text = e.Node.Text;
            label2.Text = "";
            webBrowser1.Navigate("about:blank");
            if(((Repo)e.Node.Tag).Tag != null)
                label2.Text = "Tag : " + ((Repo)e.Node.Tag).Tag;
            else if(((Repo)e.Node.Tag).Branch != null)
                label2.Text = "Branche : " + ((Repo)e.Node.Tag).Branch;
            else
                label2.Text = "";
            if (((Repo)e.Node.Tag).ServerName != null)
                label3.Text = "Serveur : " + ((Repo)e.Node.Tag).ServerName;
            else
                label3.Text = "";
            if(((Repo)e.Node.Tag).Type == "project")
            {
                // Si le noeud est un projet, autorise l'ajout de modules
                metroButton1.Enabled = true;
                toolStripStatusLabel2.Text = ((Repo)e.Node.Tag).Path;
            }
            else if(e.Node.Parent == null)
            {
                metroButton1.Enabled = false;
                toolStripStatusLabel2.Text = ((Repo)e.Node.Tag).Path;
            } 
            else
            {
                metroButton1.Enabled = false;
                toolStripStatusLabel2.Text = ((Repo)e.Node.Parent.Tag).Path;
            }
            comboBox1.SelectedItem = ((Repo)e.Node.Tag).ReadmeIndex == 1 ? "Distant" : "Local";
            try
            {
                if (!bg3IsWorking)
                {
                    // Si la tache n'est pas déjà en cours, charge le README dans le WebBrowser
                    backgroundWorker3.RunWorkerAsync(argument: ((Repo)e.Node.Tag).Id);
                }
            } catch (Exception) { }
            dataGridView1.Rows.Clear();
            dataGridView1.Refresh();
            if (((Repo)e.Node.Tag).Type == "module")
            {
                // Si le noeud sélectionné est un module :
                metroLabel4.Text = "Projets dépendant du module";
                int i = 0;
                foreach(Repo repo in repoList)
                {
                    foreach(Repo module in repo.Modules)
                    {
                        if(module.Equal((Repo)e.Node.Tag))
                        {
                            // Si le module sélectionné apparait dans les dépendances d'un projet, affiche ce projet dans le DataGridView
                            dataGridView1.Rows.Add(repo.Name, repo.Url, module.Tag, "Détails", repo.Url, repo.Id);
                        }
                    }
                    i++;
                }
            }
            else
            {
                List<Repo> submods = new List<Repo>();
                // Si le noeud sélectionné est un projet
                if (((Repo)e.Node.Tag).Localisation == Repo.Loc.local)
                {
                    submods = ((Repo)e.Node.Tag).Modules; // Récupère la liste des sous-modules locaux (lecture du fichier .gitmodules)
                }
                List<Repo> distantModules = new List<Repo>();
                metroLabel4.Text = "Modules présents dans le projet";
                int i = 0;
                if(repoList.Count != 0)
                {
                    foreach (Repo proj in repoList)
                    {
                        if (proj.Equal((Repo)e.Node.Tag))
                        {
                            // Si le noeud séléctionné est présent dans repoList
                            // Récupère la liste des sous-modules présents dans le projet distant
                            foreach (Repo module in proj.Modules)
                            {
                                if (module.IsInList(submods))
                                {
                                    // Si le module est à la fois présent localement et sur le serveur (autorise la suppression locale)
                                    dataGridView1.Rows.Add(module.Name, "Distant / Local", module.Tag, "Dépendances", module.Url, module.Id);
                                }
                                else
                                {
                                    // Si il n'est présent que sur le serveur (autorise l'affichage des dépendances)
                                    dataGridView1.Rows.Add(module.Name, "Distant", module.Tag, "Dépendances", module.Url, module.Id);
                                }
                                distantModules.Add(module);
                            }
                            break;
                        }
                        i++;
                    }
                }
                foreach (Repo submodule in submods)
                {
                    if(!submodule.IsInList(distantModules))
                    {
                        // Si le module n'est présent que localement (autorise la suppression locale)
                        dataGridView1.Rows.Add(submodule.Name, "Local", submodule.Tag, "Dépendances", submodule.Url, submodule.Id);
                    }
                }
            }
        }

        /**
         * Action à effectuer au clic sur un bouton du DataGridView
         */
        private void DataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            var senderGrid = (DataGridView)sender;

            if (senderGrid.Columns[e.ColumnIndex] is DataGridViewButtonColumn && e.RowIndex >= 0)
            {
                if ((string)senderGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value == "Détails")
                {
                    // Détails d'un projet (modules et README)
                    treeView1.SelectedNode = null;
                    string projName = dataGridView1.Rows[e.RowIndex].Cells[0].Value.ToString().Replace(".git", "");
                    metroLabel5.Text = projName;
                    toolStripStatusLabel2.Text = "";
                    try
                    {
                        if (!bg3IsWorking)
                            backgroundWorker3.RunWorkerAsync(argument: dataGridView1.Rows[e.RowIndex].Cells[5].Value.ToString());
                    }
                    catch (Exception) { }
                    dataGridView1.Rows.Clear();
                    dataGridView1.Refresh();
                    metroLabel4.Text = "Modules présents dans le projet";
                    int i = 0;
                    foreach (Repo proj in repoList)
                    {
                        if (proj.Name.Contains(projName))
                        {
                            // Récupère la liste des modules distants
                            foreach (Repo module in proj.Modules)
                            {
                                string mod = "";
                                if (module.Name.LastIndexOf(@"/") + 1 == module.Name.Length)
                                    mod = module.Name.Remove(module.Name.Length - 1);
                                else
                                    mod = module.Name;
                                dataGridView1.Rows.Add(mod.Substring(mod.LastIndexOf(@"/") + 1, mod.Length - mod.LastIndexOf(@"/") - 1), "Distant", "Dépendances");
                            }
                            break;
                        }
                        i++;
                    }
                }
                else if ((string)senderGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value == "Dépendances")
                {
                    // Détails d'un module distant (projets utilisant le module et description)
                    treeView1.SelectedNode = null;
                    string modName = dataGridView1.Rows[e.RowIndex].Cells[0].Value.ToString();
                    metroLabel5.Text = modName;
                    metroButton1.Enabled = false;
                    toolStripStatusLabel2.Text = "";
                    try
                    {
                        if (!bg3IsWorking)
                            backgroundWorker3.RunWorkerAsync(argument: dataGridView1.Rows[e.RowIndex].Cells[5].Value.ToString());
                    }
                    catch (Exception) { }
                    dataGridView1.Rows.Clear();
                    dataGridView1.Refresh();
                    metroLabel4.Text = "Projets dépendant du module";
                    int i = 0;
                    foreach (Repo proj in repoList)
                    {
                        foreach (Repo module in proj.Modules)
                        {
                            if (module.Name.Substring(module.Name.LastIndexOf(@"/") + 1, module.Name.Length - module.Name.LastIndexOf(@"/") - 1).Replace(".git", "") == modName.Substring(modName.LastIndexOf(@"/") + 1, modName.Length - modName.LastIndexOf(@"/") - 1).Replace(".git", ""))
                            {
                                dataGridView1.Rows.Add(proj.Name, "", module.Tag, "Détails", proj.Url, proj.Id);
                            }
                        }
                        i++;
                    }
                }
            }
            if (senderGrid.Columns[e.ColumnIndex] is DataGridViewLinkColumn && e.RowIndex >= 0 && senderGrid.Columns[e.ColumnIndex].HeaderText == "URL")
            {
                string url = (string)senderGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
                if (url.Contains("/r/"))
                {
                    int place = url.LastIndexOf("/");
                    Process.Start(url.Remove(place,1).Insert(place,"%2F").Replace("/r/","/summary/"));
                }
                else
                    Process.Start((string)senderGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value);
            }
        }

        /**
         * Rechercher dans le DataGridView (TODO, pas encore implémenté)
         */
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

        /**
         * Ouvre un Form pour ajouter des modules au projet sélectionné
         */
        private void MetroButton1_Click(object sender, EventArgs e)
        {
            var frm = new AddSubmodule((Repo)treeView1.SelectedNode.Tag);
            frm.Location = this.Location;
            frm.StartPosition = FormStartPosition.Manual;
            frm.FormClosed += AddModuleFormClosed; // Recharge la liste des modules quand la fenêtre est fermée
            frm.Show();
        }

        /**
         * Recharge la liste des modules locaux du projet sélectionné
         */
        private void AddModuleFormClosed(object sender, FormClosedEventArgs e)
        {
            TreeNode selectedNode = treeView1.SelectedNode;
            treeView1.SelectedNode = null;
            treeView1.SelectedNode = selectedNode;
        }

        /**
         * Supprime un module
         */
        private void BackgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            string modPath = "", projPath = "";
            treeView1.Invoke(new Action(() => modPath = ((Repo)treeView1.SelectedNode.Tag).Path));
            treeView1.Invoke(new Action(() => projPath = ((Repo)treeView1.SelectedNode.Parent.Tag).Path));
            
            Process process = new Process();
            process.StartInfo.FileName = config.GetAppData() + @"del_sub.bat";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.WorkingDirectory = projPath;
            process.StartInfo.Arguments = modPath;
            process.Start();
            e.Result = process.StandardError.ReadToEnd(); // Récupère les erreurs et warning du process
            while (!process.StandardOutput.EndOfStream)
            {
                string line = process.StandardOutput.ReadLine();
                // del_sub.bat effectue 4 opérations et affiche un status entre chaque
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
            metroTabControl2.Enabled = true;
            TreeNode node = treeView1.SelectedNode;
            treeView1.SelectedNode = null;
            node.Remove();
            
            if(e.Result.ToString().Length != 0)
            {
                // Affiche erreurs et warning dans une MessageBox
                MessageBox.Show(e.Result.ToString(), "Message", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }   
        }

        private void ToolStripSplitButton2_ButtonClick(object sender, EventArgs e)
        {
            backgroundWorker2.CancelAsync();
        }

        /**
         * Bouton "Rafraichir" : recharge tout le Form
         */
        private void ToolStripSplitButton3_ButtonClick(object sender, EventArgs e)
        {
            LoadForm();
        }

        /**
         * Cliquer sur le lien du projet sélectionné l'ouvre dans l'explorateur
         */
        private void ToolStripStatusLabel2_Click(object sender, EventArgs e)
        {
            if(toolStripStatusLabel2.Text != "")
                Process.Start(@toolStripStatusLabel2.Text);
        }

        /**
         * Action au clic sur un noeud du TreeView2 (modules)
         */
        private void TreeView2_AfterSelect(object sender, TreeViewEventArgs e)
        {
            metroLabel5.Text = e.Node.Text;
            metroButton1.Enabled = false;
            toolStripStatusLabel2.Text = "";
            label2.Text = "";
            webBrowser1.Navigate("about:blank");
            if (((Repo)e.Node.Tag).Tag != null)
                label2.Text = "Tag : " + ((Repo)e.Node.Tag).Tag;
            else if (((Repo)e.Node.Tag).Branch != null)
                label2.Text = "Branche : " + ((Repo)e.Node.Tag).Branch;
            else
                label2.Text = "";
            if (((Repo)e.Node.Tag).ServerName != null)
                label3.Text = "Serveur : " + ((Repo)e.Node.Tag).ServerName;
            else
                label3.Text = "";
            try
            {
                if (!bg3IsWorking)
                    backgroundWorker3.RunWorkerAsync(argument: ((Repo)e.Node.Tag).Id);
            } catch(Exception) { }
            dataGridView1.Rows.Clear();
            dataGridView1.Refresh();
            metroLabel4.Text = "Projets dépendant du module";
            int i = 0;
            foreach (Repo proj in repoList)
            {
                foreach (Repo mod in proj.Modules)
                {
                    if (((Repo)e.Node.Tag).Equal(mod))
                    {
                        dataGridView1.Rows.Add(proj.Name, "", mod.Tag, "Détails", proj.Url, proj.Id);
                    }
                }
                i++;
            }
        }

        /**
         * Charge un projet depuis un dépôt local
         */
        private void DepuisUnDépôtLocalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            dialog.ShowDialog();
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
                    ContextMenuStrip contextMenuStrip;
                    string chemin = path;
                    Repo repo = new Repo();
                    repo = repo.Init(chemin);

                    string name = chemin.Substring(chemin.LastIndexOf("\\") + 1, chemin.Length - chemin.LastIndexOf("\\") - 1).Replace(".git", "");

                    TreeNode treeNode = new TreeNode(name);
                    //treeNode.Name = chemin;      // Précise le chemin du projet dans le paramètre Name du noeud

                    if (repo.Type == "project")
                    {
                        List<Repo> gitmodulesLocList = functions.GetGitmodulesLoc(chemin); // Liste contenant les modules
                        int j = 0;
                        foreach (Repo submodule in gitmodulesLocList) // Ajoute chaque module en tant que fils dans l'arborescence des projets
                        {
                            TreeNode childNode = new TreeNode(submodule.Name);
                            //childNode.Name = "module";  // Précise que le neoud correspond à un module
                            childNode.Tag = submodule;

                            contextMenuStrip = new ContextMenuStrip();
                            contextMenuStrip.Items.Add("Ouvrir (local)");
                            contextMenuStrip.Items.Add("Ouvrir (URL)");
                            contextMenuStrip.Items.Add("Déplacer");
                            contextMenuStrip.Items.Add("Supprimer");
                            contextMenuStrip.ItemClicked += ContextMenuStripClick;
                            contextMenuStrip.Text = childNode.Text;
                            contextMenuStrip.Name = treeNode.Name + @"\" + childNode.Tag.ToString();
                            contextMenuStrip.Tag = submodule;
                            childNode.ContextMenuStrip = contextMenuStrip;

                            treeNode.Nodes.Add(childNode);
                            repo.Modules.Add(submodule);
                            if (!submodule.IsInList(modules))
                                modules.Add(submodule);
                            repositories.Add(submodule);
                            j++;
                        }
                    }

                    contextMenuStrip = new ContextMenuStrip();
                    contextMenuStrip.Items.Add("Ouvrir (local)");
                    contextMenuStrip.Items.Add("Ouvrir (URL)");
                    contextMenuStrip.ItemClicked += ContextMenuStripClick;
                    contextMenuStrip.Name = treeNode.Name;
                    contextMenuStrip.Tag = repo;
                    treeNode.ContextMenuStrip = contextMenuStrip;
                    treeNode.Tag = repo;
                    treeView1.Nodes.Add(treeNode);  // Ajout du projet au TreeView
                    if (!repo.IsInList(repositories))
                        repositories.Add(repo);
                }
            }
        }

        /**
         * Charge un projet depuis un dépôt distant (ouvre un nouveau Form)
         */
        private void DepuisUnURLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (LoadProjForm formOptions = new LoadProjForm())
            {
                formOptions.ShowDialog();
                try
                {
                    if (formOptions.id.Length != 0)
                    {
                        string result = formOptions.id;
                        foreach (Repo repo in repoList)
                        {
                            if(repo.Id.ToString() == result)
                            {
                                TreeNode treeNode = new TreeNode(repo.Name);
                                ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
                                contextMenuStrip.Items.Add("Ouvrir (URL)");
                                contextMenuStrip.ItemClicked += ContextMenuStripClick;
                                contextMenuStrip.Name = treeNode.Name;
                                contextMenuStrip.Tag = repo;
                                treeNode.ContextMenuStrip = contextMenuStrip;
                                treeNode.Tag = repo;
                                treeView1.Nodes.Add(treeNode);  // Ajout du projet au TreeView
                                return;
                            }
                        }
                    }
                    else if(formOptions.path != "")
                    {
                        string result = formOptions.path;
                        foreach (Repo repo in repoList)
                        {
                            if (repo.Url == result)
                            {
                                TreeNode treeNode = new TreeNode(repo.Name);
                                ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
                                contextMenuStrip.Items.Add("Ouvrir (URL)");
                                contextMenuStrip.ItemClicked += ContextMenuStripClick;
                                contextMenuStrip.Name = treeNode.Name;
                                contextMenuStrip.Tag = repo;
                                treeNode.ContextMenuStrip = contextMenuStrip;
                                treeNode.Tag = repo;
                                treeView1.Nodes.Add(treeNode);  // Ajout du projet au TreeView
                                return;
                            }
                        }

                    }
                        
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        /**
         * Récupère le README d'un dépôt et le converti en HTML
         */
        private void BackgroundWorker3_DoWork(object sender, DoWorkEventArgs e)
        {
            bg3IsWorking = true;
            string id = e.Argument.ToString();
            Console.WriteLine(id);
            string md = "";
            Repo repo = null;
            List<Repo> repos = repositories.ToList();
            repos.AddRange(repoList);
            foreach(Repo repository in repos)
            {
                if(repository.Id.ToString() == id)
                    repo = repository;
            }
            if (repo == null)
                return;
                
            string html = repo.Name; // Par défaut, affiche le nom du projet
            int readmeIndex = repo.ReadmeIndex;
            if (readmeIndex == 1)
                md = functions.GetMarkdown(repo, config.GetBranchDev()); // Récupère le markdown dans une string
            else if(repo.Path != null)
                md = functions.GetMarkdownLoc(repo.Path);

            try
            {
                // Converti le markdown en HTML
                html = Markdig.Markdown.ToHtml(md);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Markdow ToHTML error : " + ex.Message);
            }
            // TODO: Adapter le markdown à la source
            if (config.GetCurrentType() == "gitblit")
            {
                html = html.Replace("img src=\"", "img src=\"" + config.GetServerUrl() + @"raw/" + repo.Name + ".git/master/");
                html = html.Replace(@"%5C", @"/");
            }
            e.Result = html;
        }

        private void BackgroundWorker3_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // Charge le contenu HTML dans le WebBrowser
            webBrowser1.Navigate("about:blank");
            try
            {
                if (webBrowser1.Document != null)
                {
                    webBrowser1.Document.Write(string.Empty);
                }
                webBrowser1.DocumentText = e.Result.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Web browser error : " + ex.Message);
            }
            bg3IsWorking = false;
        }

        private void QuitterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /**
         * Ouvre le Form de gestion des sources
         */
        private void GérerLesSourcesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var frm = new ManageSrcForm();
            frm.Location = this.Location;
            frm.StartPosition = FormStartPosition.Manual;
            frm.Show();
        }

        /**
         * En cours de développement
         */
        private void ComptesEtConnexionsToolStripMenuItem_ClickAsync(object sender, EventArgs e)
        {

            try
            {
                Console.WriteLine(functions.Query("devops", "https://dev.azure.com/thebagalex/KIPROjects/_apis/git/repositories/PORT_SONDES_2019/items?path=_MODULES_/Atmospherique&includeContent=true&api-version=5.0",""));
            }
            catch (Exception)
            {

            }
        }

        private void ParamètresToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var frm = new Settings();
            frm.Location = this.Location;
            frm.StartPosition = FormStartPosition.Manual;
            frm.Show();
        }

        private void DossierLocalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start(((Repo)treeView1.SelectedNode.Tag).Path);
            }
            catch (Exception) { }
            
        }

        private void URLServeurToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode != null)
            {
                string path = ((Repo)treeView1.SelectedNode.Tag).Url;
                if (path == null)
                    return;
                if (path.Contains("bitbucket"))
                {

                }
                else if (path.Contains("azure") || path.Contains("visualstudio"))
                {

                }
                else
                {
                    path = path.Replace(@"/r/", @"/summary/");
                    path = path.Insert(path.LastIndexOf(@"/"), @"%2F").Replace(@"%2F/", @"%2F");
                }
                Process.Start(path);
            }
        }

        private void ContextMenuStripClick(object sender, ToolStripItemClickedEventArgs e)
        {
            switch(((ToolStripMenuItem)e.ClickedItem).ToString())
            {
                case "Ouvrir (local)":
                    DossierLocalToolStripMenuItem_Click(sender, e);
                    break;
                case "Ouvrir (URL)":
                    URLServeurToolStripMenuItem_Click(sender, e);
                    break;
                case "Déplacer":
                    DéplacerToolStripMenuItem_Click(sender, e);
                    break;
                case "Supprimer":
                    SupprimerToolStripMenuItem_Click(sender, e);
                    break;
            }
        }

        private void SupprimerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(treeView1.SelectedNode != null && ((Repo)treeView1.SelectedNode.Tag).Type == "module")
            {
                string modName = treeView1.SelectedNode.Text;
                if (MessageBox.Show("Voulez vous supprimer le module " + modName + " du projet " + treeView1.SelectedNode.Parent.Text + " ?", "Supprimer un module", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    toolStripProgressBar1.Visible = true;
                    toolStripSplitButton2.Visible = true;
                    toolStripProgressBar1.Value = 0;
                    toolStripStatusLabel1.Text = "Suppression...";
                    metroTabControl1.Enabled = false;
                    metroTabControl2.Enabled = false;
                    backgroundWorker2.RunWorkerAsync();
                }
            }
        }

        private void ComboBox1_SelectedValueChanged(object sender, EventArgs e)
        {
            try
            {
                if ((string)comboBox1.SelectedItem == "Local")
                    ((Repo)treeView1.SelectedNode.Tag).ReadmeIndex = 0;
                else
                    ((Repo)treeView1.SelectedNode.Tag).ReadmeIndex = 1;
            }
            catch (Exception) { }

            TreeNode treeNode = treeView1.SelectedNode;
            treeView1.SelectedNode = null;
            treeView1.SelectedNode = treeNode;
        }

        private void OutilsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
            if(treeView1.SelectedNode == null || ((Repo)treeView1.SelectedNode.Tag).Type != "module")
            {
                supprimerToolStripMenuItem.Enabled = false;
                déplacerToolStripMenuItem.Enabled = false;
            }
            else
            {
                supprimerToolStripMenuItem.Enabled = true;
                déplacerToolStripMenuItem.Enabled = true;
            }
        }

        private void DéplacerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode != null)
            {
                var dialog = new FolderBrowserDialog();
                dialog.SelectedPath = ((Repo)treeView1.SelectedNode.Parent.Tag).Path;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string path = dialog.SelectedPath + @"\" + treeView1.SelectedNode.Text;
                    backgroundWorker4.RunWorkerAsync(argument: path);
                }
            }
            
        }

        private void BackgroundWorker4_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            string modName = "";
            string projPath = "";
            treeView1.Invoke(new Action(() => modName = ((Repo)treeView1.SelectedNode.Tag).Name));
            treeView1.Invoke(new Action(() => projPath = ((Repo)treeView1.SelectedNode.Parent.Tag).Path));
            
            Process process = new Process();
            process.StartInfo.FileName = config.GetAppData() + "mv.bat";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.WorkingDirectory = projPath;
            process.StartInfo.Arguments ="\"" + modName + "\" \"" + e.Argument.ToString() + "\"";
            process.Start();
            e.Result = process.StandardError.ReadToEnd(); // Récupère les erreurs et warning du process
            
        }

        private void BackgroundWorker4_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
        }

        private void BackgroundWorker4_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Console.WriteLine(e.Result.ToString());
            LoadForm();
        }

        private void Label2_Click(object sender, EventArgs e)
        {

        }
    }
}

class CustomProfessionalColors : ProfessionalColorTable
{
    public override Color ToolStripGradientBegin
    { get { return Color.BlueViolet; } }

    public override Color ToolStripGradientMiddle
    { get { return Color.CadetBlue; } }

    public override Color ToolStripGradientEnd
    { get { return Color.CornflowerBlue; } }

    public override Color MenuStripGradientBegin
    { get { return SystemColors.MenuBar; } }

    public override Color MenuStripGradientEnd
    { get { return Color.White; } }
}
