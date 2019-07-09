using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace module_manager
{
    public partial class AddConfirmForm : Form
    {

        List<string> modules;   // Liste des modules à ajouter
        Functions functions;
        Config config;

        public AddConfirmForm(List<string> args)
        {
            InitializeComponent();
            modules = args;
            functions = new Functions();
            config = new Config();
        }

        private void Form3_Load(object sender, EventArgs e)
        {
            metroButton2.Enabled = false;
            toolStripStatusLabel1.Text = "Chargement...";
            backgroundWorker2.RunWorkerAsync();
        }

        /**
         * Récupère la liste des #include des fichiers .c et .h des modules séléctionnés
         */
        private void BackgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            int i = 1;
            foreach (string mod in modules)
            {
                TreeNode treeNode = new TreeNode(mod);
                treeNode.Checked = true;
                treeNode.Name = "module";
                treeView1.Invoke(new Action(() => treeView1.Nodes.Add(treeNode))); // Ajoute le module comme noeud du TreeView
                try
                {
                    List<string> dep = functions.GetModuleDep(mod, config.GetBranchDev()); // Liste des #include du module
                    List<string> allFiles = Directory.GetFiles(AddSubForm.path, "*.*", SearchOption.AllDirectories).ToList(); // Liste de tous les fichiers (locaux)
                    foreach (string dependency in dep)
                    {
                        var match = allFiles.FirstOrDefault(stringToCheck => stringToCheck.Contains(dependency));

                        if (match == null && !modules.Contains(dependency))
                        {
                            // Si le fichier #include n'est pas présent localement et s'il n'est pas dans la liste des modules à installer
                            TreeNode childNode = new TreeNode(dependency); // Créé un sous-noeud
                            if(!dependency.Contains(@"//"))
                                childNode.Checked = true; // Si le #include est en commentaire dans le code, le désélectionne
                            childNode.Name = "subInclude";
                            treeView1.Invoke(new Action(() => treeNode.Nodes.Add(childNode)));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                treeView1.Invoke(new Action(() => treeNode.ExpandAll()));
                worker.ReportProgress(i * 100 / modules.Count);
                i++;
            }
        }

        private void BackgroundWorker2_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            toolStripProgressBar1.Value = e.ProgressPercentage;
        }

        private void BackgroundWorker2_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            metroButton2.Enabled = true;
            toolStripStatusLabel1.Text = "Prêt";
            toolStripProgressBar1.Value = 0;
        }

        /**
         * Ajoute les modules au projet (lance le BackgroundWorker)
         */
        private void MetroButton2_Click(object sender, EventArgs e)
        {
            treeView1.Enabled = false;
            metroButton1.Enabled = false;
            metroButton2.Enabled = false;
            toolStripStatusLabel1.Text = "Ajout en cours...";
            backgroundWorker1.RunWorkerAsync();
        }

        private void MetroButton1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /**
         * Ajoute les éléments sélectionnés
         */
        private void BackgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            List<string> checkedNodes = functions.GetCheckedNodes(treeView1.Nodes);
            int counter = (100/checkedNodes.Count)/4;
            string mainMod = "";
            foreach (string node in checkedNodes)
            {
                //================ MODULES FOLDER NAME =====================//
                if (node.Contains("_MODULES_/"))
                    mainMod = node.Replace("_MODULES_/",""); // Nom du module en cours
                //==========================================================//
                if(AddSubForm.moduleList.FirstOrDefault(stringToCheck => stringToCheck.Contains(node.Replace(".h",""))) != null)
                {
                    if (node.Contains(".h") && MessageBox.Show("Le module [ " + node.Replace(".h", "") + " ] fait partie des dépendances du module [ " + mainMod + " ], voulez-vous l'installer ?", "Ajouter un module", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                    {
                        continue;
                    }
                    // Si un module avec le nom du noeud sélectionné existe sur le serveur, le télécharge
                    try
                    {
                        Process process = new Process();
                        //================================== CLONE PATH ============================================//
                        process.StartInfo.FileName = config.GetAppData() + @"clone.bat";
                        //==========================================================================================//
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.CreateNoWindow = true;
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        process.StartInfo.WorkingDirectory = AddSubForm.path;
                        //============================================== MODULES FOLDER NAME ==================================================//
                        string modName = node.Replace("_MODULES_/", "").Replace(".h", "");
                        if (config.GetCurrentType() == "gitblit")
                            process.StartInfo.Arguments = config.GetServerUrl() + @"r/" + "_MODULES_/" + modName + " " + "_MODULES_/" + modName;
                        //=====================================================================================================================//
                        process.Start();
                        while (!process.StandardOutput.EndOfStream)
                        {
                            string line = process.StandardOutput.ReadLine();
                            if (line == "\"status 25\"")
                            {
                                worker.ReportProgress(counter);
                                counter += (100 / checkedNodes.Count) / 4;
                            }
                            else if (line == "\"status 50\"")
                            {
                                worker.ReportProgress(counter);
                                counter += (100 / checkedNodes.Count) / 4;
                            }
                            else if (line == "\"status 75\"")
                            {
                                worker.ReportProgress(counter);
                                counter += (100 / checkedNodes.Count) / 4;
                            }
                            else if (line == "\"status 100\"")
                            {
                                worker.ReportProgress(counter);
                                counter += (100 / checkedNodes.Count) / 4;
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
                } else
                {
                    MessageBox.Show("Le module [ " + mainMod + " ] fait appel au fichier [ " + node + " ] absent du projet local et des modules distant","Fichier manquant", MessageBoxButtons.OK,MessageBoxIcon.Warning);
                    worker.ReportProgress(counter);
                    counter += (100 / checkedNodes.Count) * (3 / 4);
                    worker.ReportProgress(counter);
                }
                if (backgroundWorker1.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }
            }
        }

        private void BackgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            toolStripProgressBar1.Value = e.ProgressPercentage;
        }

        private void BackgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.Close();
        }

        private void ToolStripSplitButton1_ButtonClick(object sender, EventArgs e)
        {
            backgroundWorker1.CancelAsync();
        }

        private void TreeView1_AfterCheck(object sender, TreeViewEventArgs e)
        {
            foreach (TreeNode childNode in e.Node.Nodes)
            {
                if(!childNode.Text.Contains(@"//"))
                    childNode.Checked = e.Node.Checked;
            }
        }
    }
}
