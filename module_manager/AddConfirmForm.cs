using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;

namespace module_manager
{
    public partial class AddConfirmForm : Form
    {

        private List<string> modules;
        Functions functions;

        public AddConfirmForm(List<string> args)
        {
            InitializeComponent();
            modules = args;
            functions = new Functions();
        }

        private void Form3_Load(object sender, EventArgs e)
        {
            foreach (string mod in modules)
            {
                TreeNode treeNode = new TreeNode(mod);
                treeNode.Checked = true;
                
                treeView1.Nodes.Add(treeNode);

                List<string> dep = functions.GetModuleDep(mod, "_DEV_");

                foreach (string dependency in dep)
                {
                    if (!modules.Contains(dependency))
                    {
                        TreeNode childNode = new TreeNode(dependency);
                        childNode.Checked = true;
                        treeNode.Nodes.Add(childNode);
                    }
                }

                treeNode.ExpandAll();
            }
        }

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

        private void BackgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            List<string> checkedNodes = functions.GetCheckedNodes(treeView1.Nodes);
            int counter = (100/checkedNodes.Count)/4;
            foreach (string node in checkedNodes)
            {
                Process process = new Process();
                process.StartInfo.FileName = @"C:\Users\STBE\Downloads\PortableGit\home\TestMaster\clone.bat";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.WorkingDirectory = AddSubForm.path;
                process.StartInfo.Arguments = @"http://192.168.55.218:8082/r/" + node + " " + node;
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
                }
                process.WaitForExit();
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
    }
}
