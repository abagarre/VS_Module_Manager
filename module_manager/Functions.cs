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


    public string BitBucketQuery(string url)
    {
        //================================= CREDIT FILES PATH ==============================//
        byte[] entropy = File.ReadAllBytes(config.GetAppData() + @".creditEnt"); //TODO: Change to global password
        byte[] ciphertext = File.ReadAllBytes(config.GetAppData() + @".creditCip");
        //==================================================================================//
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        WebRequest myReq = WebRequest.Create(url);
        myReq.Method = "GET";
        CredentialCache mycache = new CredentialCache();
        myReq.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes("bglx:" + Encoding.UTF8.GetString(ProtectedData.Unprotect(ciphertext, entropy, DataProtectionScope.CurrentUser))));
        WebResponse wr = myReq.GetResponse();
        Stream receiveStream = wr.GetResponseStream();
        StreamReader reader = new StreamReader(receiveStream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    /**
     * Retourne la liste des modules présents dans le fichier .gitmodules d'un projet local
     */
    public List<string> GetGitmodulesLoc(string path)
    {
        List<string> submodules = new List<string>();
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
                            string name = line.Replace(".git", "");
                            submodules.Add(name.Substring(name.LastIndexOf("/") + 1, name.Length - name.LastIndexOf("/") - 1));
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

        return submodules;
    }

    public List<string> GetGitmodulesLocPath(string path)
    {
        List<string> submodules = new List<string>();
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
                        if (line.Contains("[submodule"))
                        {
                            string name = line.Replace("[submodule \"", "");
                            submodules.Add(name.Remove(name.LastIndexOf("\"]")));
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

        return submodules;
    }

    public List<string> BitBucketRepoList()
    {
        List<string> repoList = new List<string>();
        string url = "https://api.bitbucket.org/2.0/repositories/bglx/";
        string json = BitBucketQuery(url);
        JObject obj = JObject.Parse(json);
        JArray list = (JArray)obj["values"];
        foreach(JObject ob in list)
        {
            string name = ((string)ob["name"]).Replace(".git","");
            repoList.Add(name.Substring(name.LastIndexOf("/") + 1, name.Length - name.LastIndexOf("/") - 1));
        }

        return repoList;
    }

    public List<string> GitBlitRepoList()
    {
        List<string> repoList = new List<string>();

        string json;
        using (var client = new WebClient())
        {
            //======================================= GITBLIT RPC URL =======================//
            json = client.DownloadString(config.GetServerUrl() + "rpc/?req=LIST_REPOSITORIES");
            //===============================================================================//
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
                string name = infos["name"].ToString().Replace(".git", "");
                repoList.Add(name);
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
        return repoList;
    }

    /**
     * Retourne la liste des dépôts du serveur et rempli la liste de description
     */
    public List<string> GetRepoList()
    {
        List<string> repoList = new List<string>();
        if (config.GetCurrentType() == "gitblit")
        {
            return GitBlitRepoList();
        }
        else if(config.GetCurrentType() == "bitbucket")
        {
            return BitBucketRepoList();
        }
        return repoList;
    }

    /**
     * Retourne la liste des modules du serveur (<=> contient MODULES dans le nom du dépôt)
     */
    public List<string> GetModuleList(List<string> repoList)
    {
        List<string> moduleList = new List<string>();
        foreach(string rep in repoList)
        {
            //=============== MODULES FOLDER NAME ==================//
            /*******************************************************
                WON'T WORK BECAUSE OF THE DEFINITION OF REPOLIST
            ********************************************************/
            if (rep.Contains("MODULES"))
            //======================================================//
                moduleList.Add(rep);
        }
        return moduleList;
    }

    /**
     * Liste les fichiers distants du dépôt d'un module et récupère la liste des #include
     */
    public List<string> GetModuleDep(string moduleText, string branch)
    {
        List<string> dep = new List<string>();
        if(config.GetCurrentType() == "gitblit")
        {
            //=============================== GITBLIT RAW URL =====================//
            string url = config.GetServerUrl() + "raw/" + moduleText + "/" + branch;
            //=====================================================================//
            try {
                string html;
                using (var client = new WebClient())
                {
                    html = client.DownloadString(url);
                }
                HtmlAgilityPack.HtmlDocument document = new HtmlAgilityPack.HtmlDocument();
                document.LoadHtml(html);
                if (html.Contains("branch!") && branch != "master")
                {
                    dep.AddRange(GetModuleDep(moduleText, "master"));
                    return dep;
                }
                HtmlNodeCollection collection = document.DocumentNode.SelectNodes("//a");
                foreach (HtmlNode link in collection)
                {
                    string target = link.Attributes["href"].Value;
                    if (target.Contains(".c") || target.Contains(".h"))
                    {
                        string serverUrl = config.GetServerUrl().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        dep.AddRange(GetIncludes(serverUrl + target));
                        Console.WriteLine(serverUrl + target);
                    }
                }
            } catch (Exception) { }
            
        }
        else if(config.GetCurrentType() == "bitbucket")
        {
            string url = "https://api.bitbucket.org/2.0/repositories/bglx/" + moduleText + "/src/" + branch + "/";
            string json = BitBucketQuery(url);
            JObject obj = JObject.Parse(json);
            JArray list = (JArray)obj["values"];
            foreach (JObject ob in list)
            {
                if(((string)ob["path"]).Contains(".c") || ((string)ob["path"]).Contains(".h"))
                {
                    dep.AddRange(GetIncludes("https://bitbucket.org/bglx/" + moduleText + "/raw/" + branch + "/" + (string)ob["path"]));
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
        if(config.GetCurrentType() == "gitblit")
        {
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
                        if (!url.Contains(module.Replace(".h", "")))
                            dep.Add(module);
                    }
                }
            }
        }
        else if(config.GetCurrentType() == "bitbucket")
        {
            string result = BitBucketQuery(url);
            using (StringReader reader = new StringReader(result))
            {
                string ligne;
                while ((ligne = reader.ReadLine()) != null)
                {
                    if (!ligne.Contains("Error") && ligne.Contains("#include \""))
                    {
                        string module = ligne.Replace("\"", "").Replace("#include ", "").Replace(" ", "");
                        if (!url.Contains(module.Replace(".h", "")))
                            dep.Add(module);
                    }
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
        Form prompt = new Form
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowOnly,
            Padding = new Padding(20, 20, 20, 0),
            Size = new System.Drawing.Size(300, 200)
        };
        CheckBox chk = new CheckBox { Text = text };
        Label label = new Label
        {
            Text = caption,
            AutoSize = true
        };
        Button ok = new Button() { Text = "OK" };
        ok.Click += (sender, e) => { prompt.Close(); };
        FlowLayoutPanel panel = new FlowLayoutPanel { Dock = DockStyle.Fill };
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
    public List<string> GetSubmodList(string branch, string rep)
    {
        List<string> modList = new List<string>();
        if(config.GetCurrentType() == "gitblit")
        {
            //=============================== GITBLIT RAW URL ==============================//
            string url = config.GetServerUrl() + "raw/" + rep + "/" + branch + "/.gitmodules";
            //==============================================================================//
            try
            {
                var client = new WebClient();
                using (var stream = client.OpenRead(url))
                using (var reader = new StreamReader(stream))
                {
                    string ligne;
                    string ligne1 = "", ligne2 = "", ligne3 = "";
                    int compteur = 1;
                    while ((ligne = reader.ReadLine()) != null)
                    {
                        if (ligne.Contains("branch!") && branch != "master")
                        {
                            if (affiche)
                            {
                                affiche = !ShowDialog("Ne plus afficher", "Le projet " + rep + " ne contient pas de branche " + config.GetBranchDev() + " ! \n\nRecherche sur la branche master\n ");
                            }
                            modList.AddRange(GetSubmodList("master", rep));
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
                        if (ligne.Contains("url = "))
                        {
                            string name = ligne.Replace("url = ", "").Replace(".git", "");
                            modList.Add(name.Substring(name.LastIndexOf("/") + 1, name.Length - name.LastIndexOf("/") - 1));
                        }
                    }
                }
            } catch(Exception) { }
            
        }
        else if (config.GetCurrentType() == "bitbucket")
        {
            string result;
            try
            {
                result = BitBucketQuery("https://bitbucket.org/bglx/" + rep + "/raw/" + branch + "/.gitmodules");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                if(branch != "master")
                    modList.AddRange(GetSubmodList("master", rep));
                result = "null";
            }
            using (StringReader reader = new StringReader(result))
            {
                string ligne;
                string ligne1 = "", ligne2 = "", ligne3 = "";
                int compteur = 1;
                while ((ligne = reader.ReadLine()) != null)
                {
                    if (ligne.Contains("dashboard") && branch != "master")
                    {
                        if (affiche)
                        {
                            affiche = !ShowDialog("Ne plus afficher", "Le projet " + rep + " ne contient pas de branche " + config.GetBranchDev() + " ! \n\nRecherche sur la branche master\n ");
                        }
                        modList.AddRange(GetSubmodList("master", rep));
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
                    if (ligne.Contains("url = "))
                    {
                        string name = ligne.Replace("url = ", "").Replace(".git","");
                        modList.Add(name.Substring(name.LastIndexOf("/") + 1, name.Length - name.LastIndexOf("/") - 1 ));
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
        if(config.GetCurrentType() == "gitblit")
        {
            //========================================= GITBLIT PATH =================================================//
            fullName = fullName.Substring(fullName.IndexOf(@"/_") + 1, fullName.Length - fullName.IndexOf(@"/_") - 1);
            //========================================================================================================//
        }
        else if(config.GetCurrentType() == "bitbucket")
        {
            //========================================= BITBUCKET USERNAME ===============================================//
            fullName = fullName.Substring(fullName.IndexOf(config.GetUserName() + @"/") + 5, fullName.Length - fullName.IndexOf(@"bglx/") - 5);
            //============================================================================================================//
        }
            
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
                    //========================================= GITBLIT SOURCE FILE =====================================//
                    md = wc.DownloadString(config.GetServerUrl() + @"raw/" + projName + @".git/" + branch + @"/README.md");
                    //===================================================================================================//
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
        //=================== CREDIT FILE PATH ===========//
        File.WriteAllBytes(config.GetAppData() + @".creditEnt", entropy);
        File.WriteAllBytes(config.GetAppData() + @".creditCip", ciphertext);
        //================================================//
        
        return "OK";
    }

}
