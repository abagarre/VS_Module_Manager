//============================================================================//
//                              MAIN FORM                                     //
//                                                                            //
//============================================================================//

using Microsoft.WindowsAPICodePack.Taskbar;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace module_manager
{
    public partial class MainForm : Form
    {
        public static List<Repo> repoList;          // Liste des dépots distants
        private List<string> clientList;            // Liste des projets ouverts dans <SmartGit|SourceTree|Local>
        public static Functions functions;
        Config config;
        bool bg3IsWorking = false;                  // Etat du worker3 (charge le README)
        public static string selectedPath = "";     // Chemin du noeud sélectionné
        List<Repo> repositories = new List<Repo>(); // Liste des dépots locaux
        List<Repo> modules = new List<Repo>();      // Liste des modules
        CancellationTokenSource cancellationToken;

        public MainForm()
        {
            InitializeComponent();
            menuStrip1.Renderer = new ToolStripProfessionalRenderer(new CustomProfessionalColors());
            treeView1.NodeMouseClick += (sender, args) => treeView1.SelectedNode = args.Node;
            functions = new Functions();
            config = new Config();
            Icon = Icon.ExtractAssociatedIcon("logo.ico");
        }

        private async void Form1_LoadAsync(object sender, EventArgs e)
        {
            metroTabControl2.SelectedTab = metroTabPage3;   // Focus sur les projets ouverts
            await LoadFormAsync(false);
        }

        ///<summary>
        ///Remise à zéro de toute les listes, Labels, TreeViews, DataGridViews... Charge les dépôts dans le TreeView
        ///</summary>
        private async Task LoadFormAsync(bool fromRemote)
        {
            toolStripSplitButton3.Enabled = false;
            repoList = new List<Repo>();
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
            toolStripSplitButton1.Visible = false;
            toolStripSplitButton2.Visible = false;
            comboBox1.SelectedValue = "Local";
            webBrowser1.Navigate("about:blank");
            try
            {
                switch(config.GetClient())
                {
                    case "smartgit":
                        clientList = functions.GetSmartGitList();
                        break;
                    case "sourcetree":
                        clientList = functions.GetSourceTreetList();
                        break;
                    case "dossierlocal":
                        clientList = functions.GetLocalList(config.GetLocalRepo());
                        break;
                }
                foreach (string chemin in clientList)
                {
                    string name = chemin.Substring(chemin.LastIndexOf("\\") + 1, chemin.Length - chemin.LastIndexOf("\\") - 1).Replace(".git", "");
                    TreeNode treeNode = new TreeNode(name);
                    Repo repo = new Repo();
                    repo = repo.Init(chemin);
                    GenerateNode(repo, chemin, treeNode);
                    treeView1.Nodes.Add(treeNode);  // Ajout du projet au TreeView
                    if(!repo.IsInList(repositories))
                        repositories.Add(repo);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            if(fromRemote)
                await CreateRemoteList();
            else
                await CreateRemoteListFromFiles();
        }

        /// <summary>
        /// Recharge un noeud
        /// </summary>
        private void LoadForm(TreeNode treeNode, string chemin)
        {
            treeView1.SelectedNode = null;
            treeNode.Nodes.Clear();
            Repo repo = new Repo();
            repo = repo.Init(chemin);
            GenerateNode(repo, chemin, treeNode);
            treeView1.SelectedNode = treeNode;
        }

        /// <summary>
        /// Génère un noeud avec ses modules
        /// </summary>
        private void GenerateNode(Repo repo, string chemin, TreeNode treeNode)
        {
            ContextMenuStrip contextMenuStrip;
            if (repo.Type == "project")
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
                    modules.Add(submodule);
                    j++;
                }
            }
            contextMenuStrip = new ContextMenuStrip();
            contextMenuStrip.Items.Add("Ouvrir (local)");
            contextMenuStrip.Items.Add("Ouvrir (URL)");
            contextMenuStrip.Items.Add("Rafraichir");
            contextMenuStrip.ItemClicked += ContextMenuStripClick;
            contextMenuStrip.Name = treeNode.Name;
            contextMenuStrip.Tag = repo;
            treeNode.ContextMenuStrip = contextMenuStrip;
            treeNode.Tag = repo;
        }

        private async Task WorkAsync(Repo repo)
        {
            cancellationToken = new CancellationTokenSource();
            repo.Modules = new List<Repo>();
            // Récupère la liste des modules de ce projet et l'ajoute à la liste projList
            try
            {
                List<Repo> proj = await functions.GetSubmodListAsync(config.GetBranchDev(), repo.Name, repo);
                foreach(Repo mod in proj)
                {
                    mod.WriteRepo("module");
                }
                repo.Modules = proj;
                modules.AddRange(proj);
            }
            catch (Exception)
            {
                cancellationToken.Cancel();
            }

            if (repo.Modules.Count() == 0)
            {
                repo.Type = "module";
                modules.Add(repo);
                repo.WriteRepo("module"); // Ajoute le module au fichier modules.json
                treeView2.Invoke(new Action(() => treeView2.Nodes.Add(new TreeNode(repo.Name) { Tag = repo })));
            }
            else
            {
                repo.Type = "project";
                
            }
            repo.WriteRepo("project"); // Ajoute le projet au fichier repos.json
            repoList.Add(repo);
        }


        ///<summary>
        ///Récupère toutes les informations des projet locaux et des modules distants
        ///</summary>
        private async Task CreateRemoteList()
        {
            IProgress<int> progress = new Progress<int>(percentCompleted =>
            {
                toolStripProgressBar1.Value = percentCompleted;
                TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal);
                TaskbarManager.Instance.SetProgressValue(percentCompleted, 100, Handle);
            });

            config.ResetRepoModules();
            string currentServ = config.GetCurrentSource();
            List<string> allNames = config.GetAllNames();
            int servNb = allNames.Count();
            int j = 0;
            await Task.Run(async () =>
            {
                List<Task> listOfTasks = new List<Task>();
                foreach (string name in allNames)
                {
                    config.ChangeServer(name);
                    List<Repo> repos = await functions.GetRepoListAsync();
                    int i = 0;

                    foreach (Repo repo in repos) // Récupère la liste de tous les dépôts distants
                    {
                        listOfTasks.Add(WorkAsync(repo));
                        progress.Report(i * 100 / (servNb * repos.Count()) + j * 100 / servNb);
                        i++;
                    }
                    await Task.WhenAll(listOfTasks);
                    j++;
                }
            });
            config.ChangeServer(currentServ);
            toolStripStatusLabel1.Text = "Prêt";
            toolStripProgressBar1.Visible = false;
            toolStripSplitButton1.Visible = false;
            metroTabControl1.Enabled = true;
            treeView1.Enabled = true;
            treeView2.Enabled = true;
            TaskbarManager.Instance.SetProgressValue(0, 100, Handle);
            TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress);
            toolStripSplitButton3.Enabled = true;
        }

        private async Task CreateRemoteListFromFiles()
        {
            IProgress<int> progress = new Progress<int>(percentCompleted =>
            {
                toolStripProgressBar1.Value = percentCompleted;
                TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal);
                TaskbarManager.Instance.SetProgressValue(percentCompleted, 100, Handle);
            });
            string currentServ = config.GetCurrentSource();
            List<string> allNames = config.GetAllNames();
            int servNb = allNames.Count();
            await Task.Run(async () =>
            {
                string json = File.ReadAllText(config.GetModulePath());
                JObject conf = JObject.Parse(json);
                JArray list = (JArray)conf["modules"];
                foreach(JObject mod in list)
                {
                    Repo modl = new Repo()
                    {
                        Branch = mod["Branch"].ToString(),
                        Id = Guid.Parse(mod["Id"].ToString()),
                        Localisation = mod["Localisation"].ToString() == "local" ? Repo.Loc.local : Repo.Loc.distant,
                        Name = mod["Name"].ToString(),
                        Path = mod["Path"].ToString(),
                        ReadmeIndex = Int32.Parse(mod["ReadmeIndex"].ToString()),
                        Server = mod["Server"].ToString(),
                        ServerName = mod["ServerName"].ToString(),
                        Tag = mod["Tag"].ToString(),
                        Type = mod["Type"].ToString(),
                        Url = mod["Url"].ToString(),
                        Modules = new List<Repo>()
                    };
                    modules.Add(modl);
                    
                    if (!modl.IsInList(repoList))
                    {
                        repoList.Add(modl);
                        treeView2.Invoke(new Action(() => treeView2.Nodes.Add(new TreeNode(modl.Name) { Tag = modl })));
                    }
                        
                }

                json = File.ReadAllText(config.GetRepoPath());
                conf = JObject.Parse(json);
                list = (JArray)conf["repos"];
                int i = 0;
                foreach (JObject rep in list)
                {
                    Repo repo = new Repo()
                    {
                        Branch = rep["Branch"].ToString(),
                        Id = Guid.Parse(rep["Id"].ToString()),
                        Localisation = rep["Localisation"].ToString() == "local" ? Repo.Loc.local : Repo.Loc.distant,
                        Name = rep["Name"].ToString(),
                        Path = rep["Path"].ToString(),
                        ReadmeIndex = Int32.Parse(rep["ReadmeIndex"].ToString()),
                        Server = rep["Server"].ToString(),
                        ServerName = rep["ServerName"].ToString(),
                        Tag = rep["Tag"].ToString(),
                        Type = rep["Type"].ToString(),
                        Url = rep["Url"].ToString(),
                        Modules = new List<Repo>()
                    };
                    List<string> modList = ((JArray)rep["Modules"]).ToObject<List<string>>();
                    foreach(Repo mod in modules)
                    {
                        if(modList.Contains(mod.Id.ToString()))
                        {
                            repo.Modules.Add(mod);
                        }
                    }
                    repoList.Add(repo);
                    progress.Report(i * 100 / list.Count());
                }
            });
            config.ChangeServer(currentServ);
            toolStripStatusLabel1.Text = "Prêt";
            toolStripProgressBar1.Visible = false;
            toolStripSplitButton1.Visible = false;
            metroTabControl1.Enabled = true;
            treeView1.Enabled = true;
            treeView2.Enabled = true;
            TaskbarManager.Instance.SetProgressValue(0, 100, Handle);
            TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress);
            toolStripSplitButton3.Enabled = true;
        }


        private void ToolStripSplitButton1_ButtonClick(object sender, EventArgs e)
        {
            cancellationToken.Cancel();
        }
        

        ///<summary>
        ///Au clic sur un noeud du TreeView1 (projets) : affiche le README, charge les dépendances
        ///</summary>
        private async void TreeView1_AfterSelectAsync(object sender, TreeViewEventArgs e)
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
            if(((Repo)e.Node.Tag).Type == "project" || ((Repo)e.Node.Tag).Localisation == Repo.Loc.distant)
            {
                // Si le noeud est un projet, autorise l'ajout de modules
                metroButton1.Enabled = true;
                toolStripStatusLabel2.Text = ((Repo)e.Node.Tag).Path;
                selectedPath = ((Repo)e.Node.Tag).Path;
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
                            // Si le module sélectionné apparait dans les dépendances d'un projet, affiche ce projet dans le DataGridView
                            dataGridView1.Invoke(new Action(() => dataGridView1.Rows.Add(repo.Name, "", module.Tag ?? ((Repo)e.Node.Tag).Tag ?? module.Branch, "Détails", repo.Url, repo.Id)));
                    }
                    i++;
                }
            }
            else
            {
                List<Repo> submods = new List<Repo>();
                // Si le noeud sélectionné est un projet
                if (((Repo)e.Node.Tag).Localisation == Repo.Loc.local)
                    submods = ((Repo)e.Node.Tag).Modules; // Récupère la liste des sous-modules locaux
                List<Repo> distantModules = new List<Repo>();
                metroLabel4.Text = "Modules présents dans le projet";
                int i = 0;
                foreach (Repo proj in repoList)
                {
                    if (proj.Equal((Repo)e.Node.Tag))
                    {
                        // Si le noeud séléctionné est présent dans repoList
                        // Récupère la liste des sous-modules présents dans le projet distant
                        foreach (Repo module in proj.Modules)
                        {
                            bool added = false;
                            foreach (Repo submodule in submods)
                            {
                                if (module.Equal(submodule))
                                {
                                    // Si le module est à la fois présent localement et sur le serveur (autorise la suppression locale)
                                    dataGridView1.Invoke(new Action(() => dataGridView1.Rows.Add(module.Name, "Local / Distant", (submodule.Tag ?? submodule.Branch) + " / " + (module.Tag ?? module.Branch), "Dépendances", module.Url, module.Id)));
                                    added = true;
                                    break;
                                }
                            }
                            if (!added)
                                // Si il n'est présent que sur le serveur (autorise l'affichage des dépendances)
                                dataGridView1.Invoke(new Action(() => dataGridView1.Rows.Add(module.Name, "Distant", module.Tag ?? module.Branch, "Dépendances", module.Url, module.Id)));
                            distantModules.Add(module);
                        }
                        break;
                    }
                    i++;
                }
                foreach (Repo submodule in submods)
                {
                    if(!submodule.IsInList(distantModules))
                        // Si le module n'est présent que localement (autorise la suppression locale)
                        dataGridView1.Invoke(new Action(() => dataGridView1.Rows.Add(submodule.Name, "Local", submodule.Tag ?? submodule.Branch, "Dépendances", submodule.Url, submodule.Id)));
                }
            }
            try
            {
                if (!bg3IsWorking)
                    // Si la tache n'est pas déjà en cours, charge le README dans le WebBrowser
                    await GetMarkdownHTML(((Repo)e.Node.Tag));
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Action à effectuer au clic sur un bouton du DataGridView
        /// </summary>
        private async void DataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            var senderGrid = (DataGridView)sender;
            
            if (senderGrid.Columns[e.ColumnIndex] is DataGridViewButtonColumn && e.RowIndex >= 0)
            {
                string id = dataGridView1.Rows[e.RowIndex].Cells[5].Value.ToString();
                if ((string)senderGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value == "Détails")
                {
                    // Détails d'un projet (modules et README)
                    treeView1.SelectedNode = null;
                    string projName = dataGridView1.Rows[e.RowIndex].Cells[0].Value.ToString();
                    metroLabel5.Text = projName;
                    toolStripStatusLabel2.Text = "";
                    dataGridView1.Rows.Clear();
                    dataGridView1.Refresh();
                    metroLabel4.Text = "Modules présents dans le projet";
                    int i = 0;
                    foreach (Repo proj in repoList)
                    {
                        if (proj.Id.ToString() == id)
                        {
                            // Récupère la liste des modules distants
                            foreach (Repo module in proj.Modules)
                            {
                                dataGridView1.Rows.Add(module.Name, "Distant", module.Tag ?? module.Branch, "Dépendances", module.Url, module.Id);
                            }

                            try
                            {
                                if (!bg3IsWorking)
                                    await GetMarkdownHTML(proj);
                            }
                            catch (Exception) { }
                            break;
                        }
                        i++;
                    }
                }
                else if ((string)senderGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value == "Dépendances")
                {
                    // Détails d'un module distant (projets utilisant le module et description)

                    Repo repo = new Repo();
                    string modName = dataGridView1.Rows[e.RowIndex].Cells[0].Value.ToString();
                    foreach (Repo proj in modules)
                    {
                        if (proj.Id.ToString() == id)
                        {
                            repo = proj;
                            try
                            {
                                if (!bg3IsWorking)
                                    await GetMarkdownHTML(proj);
                            }
                            catch (Exception) { }
                            break;
                        }
                    }
                    if (repo.Name == null)
                    {
                        return;
                    }
                    treeView1.SelectedNode = null;
                    metroLabel5.Text = modName;
                    metroButton1.Enabled = false;
                    toolStripStatusLabel2.Text = "";
                    dataGridView1.Rows.Clear();
                    dataGridView1.Refresh();
                    metroLabel4.Text = "Projets dépendant du module";
                    int i = 0;
                    
                    foreach(Repo proj in repoList)
                    {
                        foreach (Repo module in proj.Modules)
                        {
                            if (module.Equal(repo))
                                dataGridView1.Rows.Add(proj.Name, "", module.Tag ?? module.Branch, "Détails", proj.Url, proj.Id);
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
                    Process.Start(url);
            }
        }

        /// <summary>
        /// Ouvre un Form pour ajouter des modules au projet sélectionné
        /// </summary>
        private void MetroButton1_Click(object sender, EventArgs e)
        {
            AddSubmodule frm = new AddSubmodule((Repo)treeView1.SelectedNode.Tag);
            frm.Location = this.Location;
            frm.StartPosition = FormStartPosition.Manual;
            frm.FormClosed += AddModuleFormClosed; // Recharge la liste des modules quand la fenêtre est fermée
            frm.Show();
        }

        /// <summary>
        /// Recharge la liste des modules locaux du projet sélectionné
        /// </summary>
        private void AddModuleFormClosed(object sender, FormClosedEventArgs e)
        {
            TreeNode selectedNode = treeView1.SelectedNode;
            treeView1.SelectedNode = null;
            selectedNode.Nodes.Clear();
            ((Repo)selectedNode.Tag).Modules.Clear();
            List<Repo> gitmodulesLocList = functions.GetGitmodulesLoc(((Repo)selectedNode.Tag).Path); // Liste contenant les modules
            int j = 0;
            foreach (Repo submodule in gitmodulesLocList) // Ajoute chaque module en tant que fils dans l'arborescence des projets
            {
                TreeNode childNode = new TreeNode(submodule.Name);
                childNode.Tag = submodule;
                ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
                contextMenuStrip.Items.Add("Ouvrir (local)");
                contextMenuStrip.Items.Add("Ouvrir (URL)");
                contextMenuStrip.Items.Add("Déplacer");
                contextMenuStrip.Items.Add("Supprimer");
                contextMenuStrip.ItemClicked += ContextMenuStripClick;
                contextMenuStrip.Text = childNode.Text;
                contextMenuStrip.Name = submodule.Path;
                contextMenuStrip.Tag = submodule;
                childNode.ContextMenuStrip = contextMenuStrip;
                selectedNode.Nodes.Add(childNode);
                ((Repo)selectedNode.Tag).Modules.Add(submodule);
                if (!submodule.IsInList(modules))
                    modules.Add(submodule);
                j++;
            }
            treeView1.SelectedNode = selectedNode;
        }

        /// <summary>
        /// Supprime un module
        /// </summary>
        private void BackgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            string modPath = "", projPath = "";
            Repo repo = new Repo();
            treeView1.Invoke(new Action(() => repo = (Repo)treeView1.SelectedNode.Tag));
            modPath = repo.Path;
            treeView1.Invoke(new Action(() => projPath = ((Repo)treeView1.SelectedNode.Parent.Tag).Path));
            string gitPath = functions.GetSubmodGitPath(repo, projPath);
            if(gitPath == null || gitPath.Length == 0)
            {
                gitPath = modPath.Substring(projPath.Length);
            }
            Process process = new Process();
            process.StartInfo.FileName = config.GetAppData() + @"del_sub.bat";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.WorkingDirectory = projPath;
            process.StartInfo.Arguments = modPath + " " + gitPath;
            process.Start();
            e.Result = process.StandardError.ReadToEnd(); // Récupère les erreurs et warning du process
            while (!process.StandardOutput.EndOfStream)
            {
                string line = process.StandardOutput.ReadLine();
                // del_sub.bat effectue 4 opérations et affiche un status entre chaque
                Console.WriteLine(line);
                switch(line)
                {
                    case "\"status 25\"":
                        worker.ReportProgress(25);
                        break;
                    case "\"status 50\"":
                        worker.ReportProgress(50);
                        break;
                    case "\"status 75\"":
                        worker.ReportProgress(75);
                        break;
                    case "\"status 100\"":
                        worker.ReportProgress(100);
                        break;
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
                // Affiche erreurs et warning dans une MessageBox
                MessageBox.Show(e.Result.ToString(), "Message", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void ToolStripSplitButton2_ButtonClick(object sender, EventArgs e)
        {
            backgroundWorker2.CancelAsync();
        }

        /// <summary>
        /// Bouton "Rafraichir" : recharge tout le Form
        /// </summary>
        private async void ToolStripSplitButton3_ButtonClickAsync(object sender, EventArgs e)
        {
            toolStripSplitButton3.Enabled = false;
            await LoadFormAsync(true);           
        }

        /// <summary>
        /// Cliquer sur le lien du projet sélectionné l'ouvre dans l'explorateur
        /// </summary>
        private void ToolStripStatusLabel2_Click(object sender, EventArgs e)
        {
            if(toolStripStatusLabel2.Text != "")
                Process.Start(@toolStripStatusLabel2.Text);
        }

        /// <summary>
        /// Action au clic sur un noeud du TreeView2 (modules)
        /// </summary>
        private async void TreeView2_AfterSelect(object sender, TreeViewEventArgs e)
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
            dataGridView1.Rows.Clear();
            dataGridView1.Refresh();
            metroLabel4.Text = "Projets dépendant du module";
            treeView2.Enabled = false;
            int i = 0;
            foreach (Repo proj in repoList)
            {
                foreach (Repo mod in proj.Modules)
                {
                    if (((Repo)e.Node.Tag).Equal(mod))
                        dataGridView1.Rows.Add(proj.Name, "", mod.Tag, "Détails", proj.Url, proj.Id);
                }
                i++;
            }
            try
            {
                if (!bg3IsWorking)
                {
                    await GetMarkdownHTML(((Repo)e.Node.Tag));
                    treeView2.Enabled = true;
                }
                    
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Charge un projet depuis un dépôt local
        /// </summary>
        private void DepuisUnDépôtLocalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            dialog.ShowDialog();
            string path = dialog.SelectedPath;
            if (path.Length != 0)
            {
                var directories = Directory.GetDirectories(path, ".git");
                if (directories.Length == 0) // Si le dossier n'est pas un repo git
                    MessageBox.Show("Le répertoire sélectionné n'est pas un dépôt Git", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                {
                    string chemin = path;
                    Repo repo = new Repo();
                    repo = repo.Init(chemin);
                    string name = chemin.Substring(chemin.LastIndexOf("\\") + 1, chemin.Length - chemin.LastIndexOf("\\") - 1).Replace(".git", "");
                    TreeNode treeNode = new TreeNode(name);
                    GenerateNode(repo, chemin, treeNode);
                    treeView1.Nodes.Add(treeNode);  // Ajout du projet au TreeView
                    if (!repo.IsInList(repositories))
                        repositories.Add(repo);
                }
            }
        }

        /// <summary>
        /// Charge un projet depuis un dépôt distant (ouvre un nouveau Form)
        /// </summary>
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

        /// <summary>
        /// Récupère le README d'un dépôt et le converti en HTML
        /// </summary>
        private async Task GetMarkdownHTML(Repo repo)
        {
            bg3IsWorking = true;
            string md = "";
            if (repo == null)
            {
                treeView1.Invoke(new Action(() => repo = ((Repo)treeView1.SelectedNode.Tag) ?? null));
                if (repo == null)
                    return;
            }
            string html = ""; // Par défaut, affiche le nom du projet
            int readmeIndex = repo.ReadmeIndex;
            if (readmeIndex == 1 || repo.Path == null)
                md = await functions.GetMarkdownAsync(repo, config.GetBranchDev()); // Récupère le markdown dans une string
            else
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
            if (config.GetCurrentType() == "gitblit")
            {
                html = html.Replace("img src=\"", "img src=\"" + config.GetServerUrl(repo.ServerName) + "raw/" + repo.Name + ".git/master/");
                html = html.Replace(@"%5C", @"/");
            }
            webBrowser1.Navigate("about:blank");
            try
            {
                if (webBrowser1.Document != null)
                    webBrowser1.Document.Write(string.Empty);
                webBrowser1.DocumentText = html;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Web browser error : " + ex.Message);
            }
            bg3IsWorking = false;
        }

        private void BackgroundWorker3_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // Charge le contenu HTML dans le WebBrowser
            webBrowser1.Navigate("about:blank");
            try
            {
                if (webBrowser1.Document != null)
                    webBrowser1.Document.Write(string.Empty);
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

        /// <summary>
        /// Ouvre le Form de gestion des sources
        /// </summary>
        private void GérerLesSourcesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ManageSrcForm frm = new ManageSrcForm();
            frm.Location = this.Location;
            frm.StartPosition = FormStartPosition.Manual;
            frm.Show();
        }

        /// <summary>
        /// Ouvre le form des paramètres
        /// </summary>
        private void ParamètresToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Settings frm = new Settings();
            frm.Location = this.Location;
            frm.StartPosition = FormStartPosition.Manual;
            frm.Show();
        }

        /// <summary>
        /// Ouvre l'explorateur de fichier au dépôt sélectionné
        /// </summary>
        private void DossierLocalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start(((Repo)treeView1.SelectedNode.Tag).Path);
            }
            catch (Exception) { }
            
        }

        /// <summary>
        /// Ouvre dans le navigateur le dépot sélectionné
        /// </summary>
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
                case "Rafraichir":
                    RafraichirToolStripMenuItem_Click(sender, e);
                    break;
            }
        }

        private void RafraichirToolStripMenuItem_Click(object sender, ToolStripItemClickedEventArgs e)
        {
            if(treeView1.SelectedNode != null)
                LoadForm(treeView1.SelectedNode, ((Repo)treeView1.SelectedNode.Tag).Path);
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
                    string path = Path.Combine(dialog.SelectedPath,treeView1.SelectedNode.Text);
                    backgroundWorker4.RunWorkerAsync(argument: path);
                }
            }
            
        }

        private void BackgroundWorker4_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            string modPath = "";
            string projPath = "";
            treeView1.Invoke(new Action(() => modPath = ((Repo)treeView1.SelectedNode.Tag).Path));
            treeView1.Invoke(new Action(() => projPath = ((Repo)treeView1.SelectedNode.Parent.Tag).Path));
            Process process = new Process();
            process.StartInfo.FileName = config.GetAppData() + "mv.bat";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.WorkingDirectory = projPath;
            process.StartInfo.Arguments ="\"" + modPath + "\" \"" + e.Argument.ToString() + "\"";
            process.Start();
            e.Result = process.StandardError.ReadToEnd(); // Récupère les erreurs et warning du process
        }

        private void BackgroundWorker4_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
        }

        private void BackgroundWorker4_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Console.WriteLine(e.Result.ToString());
            LoadForm(treeView1.SelectedNode.Parent, ((Repo)treeView1.SelectedNode.Parent.Tag).Path);
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
