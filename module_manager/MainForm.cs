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
using System.Xml.Linq;

namespace module_manager
{
    public partial class MainForm : Form
    {
        public static List<string> repoList;        // Liste des dépots distants
        public static List<List<string>> projList;  // Liste des listes des sous-modules de chaque dépôt de repoList
        private List<string> smartList;             // Liste des projets ouverts dans SmartGit
        private List<string> moduleList;
        private List<int> readmeState;
        Functions functions;
        Config config;
        bool bg3IsWorking = false;
        int treeViewIndex;

        public MainForm()
        {
            InitializeComponent();
            menuStrip1.Renderer = new ToolStripProfessionalRenderer(new CustomProfessionalColors());
            functions = new Functions();
            config = new Config();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            metroTabControl2.SelectedTab = metroTabPage3;   // Focus sur les projets ouverts
            LoadForm();
        }

        private void LoadForm()
        {
            // Remise à zéro de toute les listes, Labels, TreeViews, DataGridViews
            repoList = new List<string>();
            projList = new List<List<string>>();
            smartList = new List<string>();
            moduleList = new List<string>();
            readmeState = new List<int>();
            Functions.descList = new List<string>();
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
            comboBox1.SelectedValue = "Local";
            webBrowser1.Navigate("about:blank");
            ContextMenuStrip contextMenuStrip;
            try
            {
                // Chargement des projets ouverts dans SmartGit
                //============================ SMARTGIT REPOSITORIES XML FILE ===============================//
                XDocument xml = XDocument.Load(config.GetSmartGitRepo());
                //===========================================================================================//

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
                        string chemin = path.path.Replace(@"\\", @"\");
                        TreeNode treeNode = new TreeNode(chemin.Substring(chemin.LastIndexOf("\\") + 1, chemin.Length - chemin.LastIndexOf("\\") - 1).Replace(".git", ""));
                        treeNode.Name = chemin;      // Précise le chemin du projet dans le paramètre Name du noeud
                        contextMenuStrip = new ContextMenuStrip();
                        contextMenuStrip.Items.Add("Ouvrir (local)");
                        contextMenuStrip.Items.Add("Ouvrir (URL)");
                        contextMenuStrip.ItemClicked += ContextMenuStripClick;
                        contextMenuStrip.Name = treeNode.Name;
                        treeNode.ContextMenuStrip = contextMenuStrip;
                        treeView1.Nodes.Add(treeNode);  // Ajout du projet au TreeView
                        smartList.Add(chemin);          // Ajout du projet à la liste des projets ouverts dans SmartGit

                        int j = 0;
                        List<string> gitmodulesLocList = functions.GetGitmodulesLoc(chemin); // Liste contenant les sous-dépôts = modules
                        List<string> gitmodulesLocPathList = functions.GetGitmodulesLocPath(chemin);
                        foreach (string submodule in gitmodulesLocList) // Ajoute chaque module en tant que fils dans l'arborescence des projets
                        {
                            TreeNode childNode = new TreeNode(submodule);
                            childNode.Name = "module";  // Précise que le neoud correspond à un module
                            childNode.Tag = gitmodulesLocPathList.ElementAt(j).Replace("/",@"\");

                            contextMenuStrip = new ContextMenuStrip();
                            contextMenuStrip.Items.Add("Ouvrir (local)");
                            contextMenuStrip.Items.Add("Ouvrir (URL)");
                            contextMenuStrip.Items.Add("Supprimer");
                            contextMenuStrip.ItemClicked += ContextMenuStripClick;
                            contextMenuStrip.Text = childNode.Text;
                            contextMenuStrip.Name = treeNode.Name + @"\" + childNode.Tag.ToString();
                            contextMenuStrip.Tag = chemin;
                            childNode.ContextMenuStrip = contextMenuStrip;

                            treeNode.Nodes.Add(childNode);

                            j++;
                        }
                    }
                }
                for (int i = 0; i < treeView1.GetNodeCount(true); i++)
                {
                    readmeState.Add(1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /**
         * Récupère toutes les informations des projet locaux et des modules distants
         */
        private void BackgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            repoList = functions.GetRepoList(); // Récupère la liste de tous les dépôts distants
            int i = 1;
            foreach(string rep in repoList)
            {
                //=============== MODULES FOLDER NAME (GITBLIT) =================//
                if (rep.Contains("MODULE"))
                //===============================================================//
                    // Si le dépôt est un module, l'ajoute au TreeView des modules
                    treeView2.Invoke(new Action(() => treeView2.Nodes.Add(new TreeNode(rep.Replace(".git", "")))));
               
                // Récupère la liste des modules de ce projet et l'ajoute à la liste projList
                try
                {
                    List<string> proj = functions.GetSubmodList(config.GetBranchDev(), rep);
                    projList.Add(proj);
                }
                catch (Exception)
                {
                    // Si pas de modules, ajoute liste vide
                    projList.Add(new List<string>());
                }
                worker.ReportProgress((i * 100) / repoList.Count);
                i++;
            }
            
            if (backgroundWorker1.CancellationPending)
            {
                e.Cancel = true;
                return;
            }       
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
        private void ToolStripSplitButton1_ButtonClick(object sender, EventArgs e)
        {
            backgroundWorker1.CancelAsync();
        }

        /**
         * Au clic sur un noeud du TreeView1 (projets) :
         *      - Affiche la description + README
         *      - Charge les dépendances
         */
        private void TreeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            metroLabel5.Text = e.Node.Text;
            metroLabel6.Text = "";
            webBrowser1.Navigate("about:blank");
            if(e.Node.Name != "module")
            {
                // Si le noeud est un projet, autorise l'ajout de modules
                metroButton1.Enabled = true;
                toolStripStatusLabel2.Text = e.Node.Name;
            } else
            {
                metroButton1.Enabled = false;
                toolStripStatusLabel2.Text = e.Node.Parent.Name;
            }
            int treeIndex = functions.GetIndex(treeView1.SelectedNode);
            treeViewIndex = treeIndex;
            comboBox1.SelectedItem = readmeState.ElementAt(treeIndex) == 1 ? "Distant" : "Local";
            int item = 0;
            foreach(string rep in repoList)
            {
                if(rep.Substring(rep.LastIndexOf(@"/") + 1, rep.Length - rep.LastIndexOf(@"/") - 1) == e.Node.Text)
                {
                    // Recherche dans la liste des dépôts celui qui a été selectionné
                    metroLabel6.Text = Functions.descList.ElementAt(item);  // Récupère la description dans la liste (même taille que repoList)
                    try
                    {
                        if(!bg3IsWorking)
                            // Si la tache n'est pas déjà en cours, charge le README dans le WebBrowser
                            backgroundWorker3.RunWorkerAsync(argument: rep);
                    } catch (Exception) { }

                    break;
                }
                item++;
            }
            dataGridView1.Rows.Clear();
            dataGridView1.Refresh();
            if (e.Node.Name == "module")
            {
                // Si le noeud sélectionné est un module :
                metroLabel4.Text = "Projets dépendants du module";
                int i = 0;
                foreach(List<string> proj in projList)
                {
                    foreach(string module in proj)
                    {
                        if(module == e.Node.Text)
                        {
                            // Si le module sélectionné apparait dans les dépendances d'un projet, affiche ce projet dans le DataGridView
                            dataGridView1.Rows.Add(repoList.ElementAt(i), "", "Détails");
                        }
                    }
                    i++;
                }
            }
            else
            {
                // Si le noeud sélectionné est un projet
                List<string> gitmodulesLocList = functions.GetGitmodulesLoc(@toolStripStatusLabel2.Text); // Récupère la liste des sous-modules locaux (lecture du fichier .gitmodules)
                List<string> distantModules = new List<string>();
                metroLabel4.Text = "Modules présents dans le projet";
                int i = 0;
                foreach(List<string> proj in projList)
                {
                    if (repoList.ElementAt(i).IndexOf(e.Node.Text, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // Si le noeud séléctionné est présent dans repoList.ElementAt(i)
                        // Récupère la liste des sous-modules présents dans le projet distant
                        foreach (string module in proj)
                        {
                            if(gitmodulesLocList.Contains(module))
                            {
                                // Si le module est à la fois présent localement et sur le serveur (autorise la suppression locale)
                                dataGridView1.Rows.Add(module, "Distant / Local", "Dépendances");
                            }
                            else
                            {
                                // Si il n'est présent que sur le serveur (autorise l'affichage des dépendances)
                                dataGridView1.Rows.Add(module, "Distant", "Dépendances");
                            }
                            distantModules.Add(module);
                        }
                        break;
                    }
                    i++;
                }

                foreach (string submodule in gitmodulesLocList)
                {
                    if(!distantModules.Contains(submodule))
                    {
                        // Si le module n'est présent que localement (autorise la suppression locale)
                        dataGridView1.Rows.Add(submodule, "Local", "Dépendances");
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
                /* if((string) senderGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value == "Supprimer")
                {
                    // Supprimer un module
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
                else */if ((string)senderGrid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value == "Détails")
                {
                    // Détails d'un projet (modules et description)
                    treeView1.SelectedNode = null;
                    string projName = dataGridView1.Rows[e.RowIndex].Cells[0].Value.ToString().Replace(".git", "");
                    metroLabel5.Text = projName;
                    toolStripStatusLabel2.Text = "";
                    int item = 0;
                    foreach (string rep in repoList)
                    {
                        if (rep.Contains(projName))
                        {
                            try
                            {
                                metroLabel6.Text = Functions.descList.ElementAt(item);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex);
                            }
                            
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
                            // Récupère la liste des modules distants
                            foreach (string module in proj)
                            {
                                string mod = "";
                                if (module.LastIndexOf(@"/") + 1 == module.Length)
                                    mod = module.Remove(module.Length - 1);
                                else
                                    mod = module;
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
                    string modName = dataGridView1.Rows[e.RowIndex].Cells[0].Value.ToString();
                    metroLabel5.Text = modName;
                    metroButton1.Enabled = false;
                    toolStripStatusLabel2.Text = "";
                    int item = 0;
                    foreach (string rep in repoList)
                    {
                        if (rep.Contains(modName))
                        {
                            // Récupère la description et le README
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
                            if (module.Substring(module.LastIndexOf(@"/") + 1, module.Length - module.LastIndexOf(@"/") - 1).Replace(".git", "") == modName.Substring(modName.LastIndexOf(@"/") + 1, modName.Length - modName.LastIndexOf(@"/") - 1).Replace(".git", ""))
                            {
                                dataGridView1.Rows.Add(repoList.ElementAt(i), "", "Détails");
                            }
                        }
                        i++;
                    }
                }
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
            string[] arg = { toolStripStatusLabel2.Text };
            var frm = new AddSubForm(arg);
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
            //BackgroundWorker worker = sender as BackgroundWorker;
            string arg = e.Argument.ToString();
            string modPath = arg.Substring(0, arg.IndexOf("___"));
            string projPath = arg.Substring(arg.IndexOf("___") + 3, arg.Length - arg.IndexOf("___") - 3);
            //Console.WriteLine(modPath + " " + projPath + " " + modPath.Substring(projPath.Length, modPath.Length - projPath.Length));
            /*
            Process process = new Process();
            //============================================ DEL SUB PATH ==================================//
            process.StartInfo.FileName = config.GetAppData() + @"del_sub.bat";
            //============================================================================================//
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.WorkingDirectory = @toolStripStatusLabel2.Text;
            process.StartInfo.Arguments = modPath + " " + projPath + " " + modPath.Substring(projPath.Length,modPath.Length - projPath.Length);
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
            */
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
                    if (module.Substring(module.LastIndexOf(@"/") + 1, module.Length - module.LastIndexOf(@"/") - 1).Replace(".git", "") == e.Node.Text.Substring(e.Node.Text.LastIndexOf(@"/") + 1, e.Node.Text.Length - e.Node.Text.LastIndexOf(@"/") - 1).Replace(".git", ""))
                    {
                        dataGridView1.Rows.Add(repoList.ElementAt(i), "", "Détails");
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
                    // Ajoute un noeud au TreeView1 (projet + modules locaux)
                    TreeNode treeNode = new TreeNode(path.Substring(path.LastIndexOf("\\") + 1, path.Length - path.LastIndexOf("\\") - 1));
                    treeNode.Name = path;
                    treeView1.Nodes.Add(treeNode);
                    smartList.Add(path);

                    List<string> gitmodulesLocList = functions.GetGitmodulesLoc(path); // Liste contenant les subrepo = modules
                    foreach (string submodule in gitmodulesLocList) // Pour chaque module, créé un bouton redirigeant vers la page du module
                    {
                        TreeNode childNode = new TreeNode(submodule);
                        childNode.Name = "module";
                        treeNode.Nodes.Add(childNode);
                    }
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

        /**
         * Récupère le README d'un dépôt et le converti en HTML
         */
        private void BackgroundWorker3_DoWork(object sender, DoWorkEventArgs e)
        {
            bg3IsWorking = true;
            string project = (string) e.Argument;
            string md = "";
            string path = "";
            TreeNode treeNode = new TreeNode(); ;
            string html = project; // Par défaut, affiche le nom du projet
            /*
            int treeIndex = functions.GetIndex(treeView1.SelectedNode);
            Console.WriteLine("index : " + treeIndex);
            */
            if (readmeState.ElementAt(treeViewIndex) == 1) 
                md = functions.GetMarkdown(project, config.GetBranchDev()); // Récupère le markdown dans une string
            else
            {
                treeView1.Invoke(new Action(() => treeNode = treeView1.SelectedNode.Parent));
                if (treeNode == null)
                {
                    treeView1.Invoke(new Action(() => path = treeView1.SelectedNode.Name));
                    md = functions.GetMarkdownLoc(path);
                }
                else
                {
                    treeView1.Invoke(new Action(() => path = treeView1.SelectedNode.Parent.Name));
                    List<string> gitmodulesLocPathList = functions.GetGitmodulesLocPath(path);
                    foreach (string submodule in gitmodulesLocPathList)
                    {
                        if(submodule.Contains(project.Substring(project.LastIndexOf("/") + 1, project.Length - project.LastIndexOf("/") - 1)))
                        {
                            md = functions.GetMarkdownLoc(path + @"\" + submodule.Replace(@"/", @"\"));
                        }
                            
                    }
                }
                
                
            }
                

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
            if(config.GetCurrentType() == "gitblit")
            {
                html = html.Replace("img src=\"", "img src=\"" + config.GetServerUrl() + @"raw/" + project + ".git/master/");
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
        private void ComptesEtConnexionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            /*
            //string url = "https://api.bitbucket.org/2.0/repositories/bglx/pressure/src/master/";
            //string url = "https://bitbucket.org/bglx/pressure/raw/master/Pressure.c";
            List<string> modl = functions.GetModuleDep("pressure","master");
            foreach(string mod in modl)
            {
                Console.WriteLine(mod);
            }
            
            string json = File.ReadAllText(@"C:\Users\STBE\Downloads\PortableGit\home\PORT_DISTRI_2019\.git\index");
            byte[] bytes = Encoding.Default.GetBytes(json);
            json = Encoding.UTF8.GetString(bytes);
            Console.WriteLine(json)*/
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
            if(treeView1.SelectedNode.Name == "module")
            {
                if (toolStripStatusLabel2.Text != "")
                    Process.Start(@toolStripStatusLabel2.Text + @"\" + treeView1.SelectedNode.Tag.ToString());
            }
            else
            {
                if (toolStripStatusLabel2.Text != "")
                    Process.Start(@toolStripStatusLabel2.Text);
            }
        }

        private void URLServeurToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string url = "";
            Process.Start(url);
        }

        private void ContextMenuStripClick(object sender, ToolStripItemClickedEventArgs e)
        {
            switch(((ToolStripMenuItem)e.ClickedItem).ToString())
            {
                case "Ouvrir (local)":
                    Process.Start(((ContextMenuStrip)sender).Name);
                    break;
                case "Supprimer":
                    string modName = ((ContextMenuStrip)sender).Text;
                    if (MessageBox.Show("Voulez vous supprimer le module " + modName + "?", "Supprimer un module", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        toolStripProgressBar1.Visible = true;
                        toolStripSplitButton2.Visible = true;
                        toolStripProgressBar1.Value = 0;
                        toolStripStatusLabel1.Text = "Suppression...";
                        metroTabControl1.Enabled = false;
                        string arg = ((ContextMenuStrip)sender).Name.Replace(@"\",@"\\") + "___" + ((ContextMenuStrip)sender).Tag.ToString().Replace(@"\", @"\\");
                        Console.WriteLine(@arg);
                        try
                        {
                            backgroundWorker2.RunWorkerAsync(argument: arg);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                        
                    }
                    break;
            }
        }

        private void SupprimerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(treeView1.SelectedNode.Name == "module")
            {
                string modName = treeView1.SelectedNode.Text;
                if (MessageBox.Show("Voulez vous supprimer le module " + modName + " du projet " + treeView1.SelectedNode.Parent.Text + " ?", "Supprimer un module", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    toolStripProgressBar1.Visible = true;
                    toolStripSplitButton2.Visible = true;
                    toolStripProgressBar1.Value = 0;
                    toolStripStatusLabel1.Text = "Suppression...";
                    metroTabControl1.Enabled = false;
                    backgroundWorker2.RunWorkerAsync(argument: treeView1.SelectedNode.Tag.ToString() + ":::" + treeView1.SelectedNode.Parent.Name);
                }
            }
        }

        private void ComboBox1_SelectedValueChanged(object sender, EventArgs e)
        {
            try
            {
                int index = functions.GetIndex(treeView1.SelectedNode);
                if ((string)comboBox1.SelectedItem == "Local")
                    readmeState[index] = 0;
                else
                    readmeState[index] = 1;
            }
            catch (Exception) { }

            TreeNode treeNode = treeView1.SelectedNode;
            treeView1.SelectedNode = null;
            treeView1.SelectedNode = treeNode;
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
    { get { return SystemColors.ControlLight; } }

    public override Color MenuStripGradientEnd
    { get { return Color.White; } }
}
