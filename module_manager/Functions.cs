using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Windows.Forms;
using System.Text;
using System.Security.Cryptography;
using module_manager;
using HtmlAgilityPack;

public class Functions
{
    
    public static bool affiche = false;
    public static List<string> descList = new List<string>();
    Config config = new Config();

    /**
     * Retourne la liste des modules présents dans le fichier .gitmodules d'un projet local
     */
    public List<string> SearchGitmodulesFile(string path)
    {
        List<string> repo = new List<string>();
        try
        {
            foreach (string f in Directory.GetFiles(path))
            {
                if (f.Contains(".gitmodules"))
                {
                    int counter = 0;
                    string line;
                    StreamReader file = new StreamReader(f);
                    while ((line = file.ReadLine()) != null)
                    {
                        if (line.Contains("url"))
                        {
                            repo.Add(line.Replace("url = ", ""));
                        }
                        counter++;
                    }
                    file.Close();
                }
            }
        }
        catch (Exception excpt)
        {
            Console.WriteLine(excpt.Message);
        }

        return repo;
    }

    /**
     * Retourne la liste des dépôts du serveur et rempli la liste de description
     */
    public List<string> DispRepoList()
    {
        List<string> repoList = new List<string>();
        if (config.GetCurrentType() == "gitblit")
        {
            string json;
            using (var client = new WebClient())
            {
                json = client.DownloadString(config.GetServerUrl() + "rpc/?req=LIST_REPOSITORIES");
            }

            byte[] bytes = Encoding.Default.GetBytes(json);
            json = Encoding.UTF8.GetString(bytes);

            JObject list = JObject.Parse(json);
            var properties = list.Properties();
            foreach (var prop in properties)
            {
                JObject infos = JObject.FromObject(list[prop.Name]);
                if (!((string)infos["name"]).Contains("~"))
                {
                    repoList.Add(infos["name"].ToString());
                    try
                    {
                        string desc = infos["description"].ToString();
                        descList.Add(desc);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }
        return repoList;
    }

    /**
     * Retourne la liste des modules du serveur (<=> contient MODULES dans le nom du dépôt)
     */
    public List<string> DispModList()
    {
        List<string> repoList = new List<string>();
        if (config.GetCurrentType() == "gitblit")
        {
            string json;
            using (var client = new WebClient())
            {
                json = client.DownloadString(config.GetServerUrl() + "rpc/?req=LIST_REPOSITORIES");
            }

            byte[] bytes = Encoding.Default.GetBytes(json);
            json = Encoding.UTF8.GetString(bytes);

            JObject list = JObject.Parse(json);
            var properties = list.Properties();
            foreach (var prop in properties)
            {
                JObject infos = JObject.FromObject(list[prop.Name]);
                if (!((string)infos["name"]).Contains("~") && ((string)infos["name"]).Contains("MODULES"))
                {
                    repoList.Add(infos["name"].ToString());
                    /*
                    try
                    {
                        string desc = infos["description"].ToString();
                        descList.Add(desc);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    */
                }

            }
        }
        return repoList;
    }

    /**
     * Retoure la string du fichier des serveurs enregistrés
     */
    public string DispServerList()
    {
        List<string> servList = new List<string>();
        string json;
        json = File.ReadAllText(@"C:\Users\STBE\source\repos\module_manager\module_manager\bin\Debug\servers.json");

        byte[] bytes = Encoding.Default.GetBytes(json);
        json = Encoding.UTF8.GetString(bytes);
        
        return json;
    }

    /**
     * Liste les fichiers distants du dépôt d'un module et récupère la liste des #include
     */
    public List<string> GetModuleDep(string moduleText, string branch)
    {
        List<string> dep = new List<string>();
        if(config.GetCurrentType() == "gitblit")
        {
            string url = config.GetServerUrl() + "raw/" + moduleText + "/" + branch;

            string html;
            using (var client = new WebClient())
            {
                html = client.DownloadString(url);
            }
            HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
            document.LoadHtml(html);
            if (html.Contains("branch") && branch != "master")
            {
                dep.AddRange(GetModuleDep(moduleText, "master"));
                return dep;
            }
            HtmlNodeCollection collection = document.DocumentNode.SelectNodes("//a");
            foreach (HtmlNode link in collection)
            {
                string target = link.Attributes["href"].Value;
                if(target.Contains(".c") || target.Contains(".h"))
                {
                    string serverUrl = config.GetServerUrl().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    dep.AddRange(GetIncludes(serverUrl + target));
                }
            }
        }

        return dep;
    }

    /**
     * Retourne la liste des #include d'un fichier distant
     */
    public List<string> GetIncludes(string url)
    {
        List<string> dep = new List<string>();
        var client = new WebClient();
        using (var stream = client.OpenRead(url))
        using (var reader = new StreamReader(stream))
        {
            string ligne;
            while ((ligne = reader.ReadLine()) != null)
            {
                if (!ligne.Contains("Error") && ligne.Contains("#include \""))
                {
                    string module = ligne.Replace("\"", "").Replace("#include ", "").Replace(" ", "");
                    if(!url.Contains(module.Replace(".h","")))
                        dep.Add(module);
                }
            }
        }
        return dep;
    }

    /**
     * Ouvre une fenêtre lorsqu'un dépôt n'a pas de branche _DEV_
     */
    public static bool ShowDialog(string text, string caption)
    {
        Form prompt = new Form();
        prompt.AutoSize = true;
        prompt.AutoSizeMode = AutoSizeMode.GrowOnly;
        prompt.Padding = new Padding(20, 20, 20, 0);
        prompt.Size = new System.Drawing.Size(300, 200);
        FlowLayoutPanel panel = new FlowLayoutPanel();
        panel.Dock = DockStyle.Fill;
        CheckBox chk = new CheckBox();
        chk.Text = text;
        Label label = new Label();
        label.Text = caption;
        label.AutoSize = true;
        Button ok = new Button() { Text = "OK" };
        ok.Click += (sender, e) => { prompt.Close(); };
        panel.Controls.Add(label);
        panel.Controls.Add(chk);
        panel.SetFlowBreak(chk, true);
        panel.Controls.Add(ok);
        prompt.Controls.Add(panel);
        prompt.ShowDialog();
        return chk.Checked;
    }

    /**
     * Retourne la liste des modules présents dans le fichier .gitmodules d'un projet distant
     */
    public List<string> GetModList(string branch, string rep)
    {
        List<string> modList = new List<string>();
        if(config.GetCurrentType() == "gitblit")
        {
            string url = config.GetServerUrl() + "raw/" + rep + "/" + branch + "/.gitmodules";
            var client = new WebClient();
            using (var stream = client.OpenRead(url))
            using (var reader = new StreamReader(stream))
            {
                string ligne;
                string ligne1 = "", ligne2 = "", ligne3 = "";
                int compteur = 1;
                while ((ligne = reader.ReadLine()) != null)
                {
                    if (ligne.Contains("branch") && branch != "master")
                    {
                        if (affiche)
                        {
                            affiche = !ShowDialog("Ne plus afficher", "Le projet " + rep + " ne contient pas de branche _DEV_ ! \n\nRecherche sur la branche master\n ");
                        }
                        modList.AddRange(GetModList("master", rep));
                        break;
                    }
                    switch (compteur)
                    {
                        case 1:
                            ligne1 = ligne;
                            break;
                        case 2:
                            ligne2 = ligne;
                            break;
                        case 3:
                            ligne3 = ligne;
                            compteur = 1;
                            break;
                    }
                    if (ligne.Contains("url"))
                    {
                        modList.Add(ligne.Replace("url = ", ""));
                    }
                }
            }
        }
        return modList;
    }

    /**
     * Retourne le nom du projet avec le dossier qui le contient
     */
    public string GetProjFullName(string path)
    {
        string line = "";
        string prev = "";
        string fullName = "";
        StreamReader file = new StreamReader(path + @"\.git\config");
        while ((line = file.ReadLine()) != null)
        {
            if (line.Contains("remote"))
            {
                prev = line;
            }
            if(prev.Contains("remote") && line.Contains("url"))
            {
                fullName = line.Replace("url = ","");
                break;
            }
        }
        file.Close();
        fullName = fullName.Substring(fullName.IndexOf(@"/r/") + 3, fullName.Length - fullName.IndexOf(@"/r/") - 3);
        return fullName;

    }

    /**
     * Retourne la liste des neouds cochés dans un TreeView
     */
    public List<string> GetCheckedNodes(TreeNodeCollection treeNode)
    {
        List<string> checkedList = new List<string>();

        foreach (TreeNode node in treeNode)
        {
            if (node.Checked)
            {
                checkedList.Add(node.Text);
            }

            if(node.Nodes.Count != 0)
            {
                checkedList.AddRange(GetCheckedNodes(node.Nodes));
            }
        }

        return checkedList;
    }

    /**
     * Retourne le contenu d'un fichier README dans une chaîne de caractères
     */
    public string GetMarkdown(string projName, string branch)
    {
        string md = "";
        if (config.GetCurrentType() == "gitblit")
        {
            try
            {
                using (var wc = new WebClient())
                    md = wc.DownloadString(config.GetServerUrl() + @"raw/" + projName + @".git/" + branch + @"/README.md");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed downloading README.md : " + ex.Message);
            }
            if (md.Contains("branch") && branch != "master")
            {
                return GetMarkdown(projName, "master");
            }
            byte[] bytes = Encoding.Default.GetBytes(md);
            md = Encoding.UTF8.GetString(bytes);
        }
        return md;
    }

    /**
     * 
     */
    public string SavePassword()
    {
        byte[] plaintext = Encoding.UTF8.GetBytes("");

        byte[] entropy = new byte[20]; //TODO: Change to global password (winform with textbox)
        using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
        {
            rng.GetBytes(entropy);
        }

        byte[] ciphertext = ProtectedData.Protect(plaintext, entropy, DataProtectionScope.CurrentUser);

        File.WriteAllBytes(@"./.creditEnt", entropy);
        File.WriteAllBytes(@"./.creditCip", ciphertext);
        
        return "OK";
    }

}
