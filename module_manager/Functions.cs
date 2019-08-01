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
using System.Xml.Linq;
using System.Linq;
using System.Web;
using System.Diagnostics;
using LibGit2Sharp;

public class Functions
{

    public static bool affiche = false;
    Config config = new Config();
    string entropy = "";

    public string Query(string type, string url, string source)
    {
        if (entropy == "")
        {
            using (Password formOptions = new Password())
            {
                formOptions.ShowDialog();
                formOptions.Activate();
                try
                {
                    if (formOptions.pass.Length != 0)
                        entropy = formOptions.pass;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
        try
        {
            byte[] ciphertext = File.ReadAllBytes(config.GetAppData() + @".cred" + source);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            WebRequest myReq = WebRequest.Create(url);
            myReq.Method = "GET";
            CredentialCache mycache = new CredentialCache();
            if(type == "bitbucket")
            {
                myReq.Headers["Authorization"] = "Basic " + Convert.ToBase64String(
                    Encoding.ASCII.GetBytes(
                        config.GetUserName(source) + ":" + Encoding.UTF8.GetString(ProtectedData.Unprotect(ciphertext, Encoding.Default.GetBytes(entropy), DataProtectionScope.CurrentUser))));

            } else if(type == "devops")
            {
                myReq.Headers["Authorization"] = "Basic " + Convert.ToBase64String(
                    ASCIIEncoding.ASCII.GetBytes(
                        string.Format("{0}:{1}", "", Encoding.UTF8.GetString(ProtectedData.Unprotect(ciphertext, Encoding.Default.GetBytes(entropy), DataProtectionScope.CurrentUser)))));
            }
            WebResponse wr = myReq.GetResponse();
            Stream receiveStream = wr.GetResponseStream();
            StreamReader reader = new StreamReader(receiveStream, Encoding.UTF8);
            return reader.ReadToEnd();
        }
        catch (CryptographicException ex)
        {
            entropy = "";
            if (MessageBox.Show("Mot de passe incorrect", "Erreur", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == DialogResult.Retry)
                Query(type,url, source);
            Console.WriteLine(type + " query error : " + ex.Message);
        }
        catch (WebException) { }
        return "";

    }

    /**
     * Retourne la liste des modules présents dans le fichier .gitmodules d'un projet local
     */
    public List<Repo> GetGitmodulesLoc(string path)
    {
        List<Repo> repos = new List<Repo>();
        try
        {
            if(File.Exists(Path.Combine(path,".gitmodules")))
            {
                string line;
                string subpath = "";
                string Name = null, Server = null, Url = null, Path = null, Type = null, Tag = null, Branch = null;
                StreamReader file = new StreamReader(System.IO.Path.Combine(path, ".gitmodules"));
                Repo repo = new Repo();
                while ((line = file.ReadLine()) != null)
                {
                    if (line.Contains("submodule"))
                    {
                        if (Name != null)
                        {
                            if (Branch == null || Tag == null)
                            {
                                using (var reposit = new Repository(Path))
                                {
                                    foreach (var tags in reposit.Tags)
                                    {
                                        if (tags.PeeledTarget.Id == reposit.Head.Tip.Id)
                                            Tag = tags.FriendlyName;
                                    }
                                    Branch = reposit.Head.FriendlyName;
                                    Console.WriteLine(Name + " : " + Branch + " - " + Tag);
                                }

                            }
                            repo = new Repo()
                            {
                                Id = Guid.NewGuid(),
                                Name = Name,
                                Server = Server,
                                Url = Url,
                                Path = Path,
                                Type = Type,
                                Tag = Tag,
                                Branch = Branch,
                                ReadmeIndex = 0,
                                Localisation = Repo.Loc.local,
                            };
                            repo.ServerName = GetServerName(repo);
                            repos.Add(repo);
                            Name = null; Server = null; Url = null; Path = null; Type = null; Tag = null; Branch = null;
                        }
                        repo = new Repo();
                        Type = "module";
                        subpath = line.Replace("[submodule \"", "").Replace("\"]", "").Trim().Replace("/", @"\");
                    }
                    if (line.Contains("path"))
                    {
                        Path = System.IO.Path.Combine(path,line.Replace("path = ", "").Trim().Replace("/",@"\"));
                    }
                    if (line.Contains("url"))
                    {
                        Name = line.Substring(line.LastIndexOf("/") + 1, line.Length - line.LastIndexOf("/") - 1).Replace(".git", "");
                        Url = line.Replace("url = ", "");
                        if (Url.Contains("azure") || Url.Contains("visualstudio"))
                            Server = "devops";
                        else if (Url.Contains("bitbucket"))
                            Server = "bitbucket";
                        else
                            Server = "gitblit";
                    }
                }
                file.Close();

                if (Name != null)
                {
                    if (Branch == null || Tag == null)
                    {
                        using (var reposit = new Repository(Path))
                        {
                            foreach (var tags in reposit.Tags)
                            {
                                if (tags.PeeledTarget.Id == reposit.Head.Tip.Id)
                                    Tag = tags.FriendlyName;
                            }
                            Branch = reposit.Head.FriendlyName;
                            Console.WriteLine(Name + " : " + Branch + " - " + Tag);
                        }
                    }
                    repo = new Repo()
                    {
                        Id = Guid.NewGuid(),
                        Name = Name,
                        Server = Server,
                        Url = Url,
                        Path = Path,
                        Type = Type,
                        Tag = Tag,
                        Branch = Branch,
                        ReadmeIndex = 0,
                        Localisation = Repo.Loc.local,
                    };
                    repo.ServerName = GetServerName(repo);
                    repos.Add(repo);
                }
            }
        }
        catch (Exception excpt)
        {
            Console.WriteLine(excpt.Message);
        }

        return repos;
    }

    internal List<string> GetLocalList(string path)
    {
        List<string> localList = new List<string>();
        string[] subdir = Directory.GetDirectories(path);
        foreach(string dir in subdir)
        {
            if(Directory.Exists(Path.Combine(dir,".git")))
                localList.Add(dir);
            else
                localList.AddRange(GetLocalList(dir));
        }
        return localList; 
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
                        if (line.Contains("path = "))
                        {
                            string name = line.Replace("path = ", "").Trim();
                            submodules.Add(name);
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

    public List<Repo> BitBucketRepoList()
    {
        List<Repo> repos = new List<Repo>();
        string url = "https://api.bitbucket.org/2.0/repositories/" + config.GetUserName();
        string json = Query("bitbucket",url, config.GetCurrentSource());
        if(json != "")
        {
            JObject obj = JObject.Parse(json);
            JArray list = (JArray)obj["values"];
            foreach (JObject ob in list)
            {
                Repo repo = new Repo
                {
                    Id = Guid.NewGuid(),
                    Name = ob["name"].ToString(),
                    Url = ob["links"]["html"]["href"].ToString(),
                    Branch = ob["mainbranch"]["name"].ToString(),
                    Localisation = Repo.Loc.distant,
                    ReadmeIndex = 1,
                    Server = "bitbucket",
                    ServerName = config.GetCurrentSource()
                };
                repos.Add(repo);
            }
        }
        return repos;
    }

    public List<Repo> GitBlitRepoList()
    {
        List<Repo> repos = new List<Repo>();
        
        string json;
        using (var client = new WebClient())
            json = client.DownloadString(config.GetServerUrl() + "rpc/?req=LIST_REPOSITORIES");

        byte[] bytes = Encoding.Default.GetBytes(json);
        json = Encoding.UTF8.GetString(bytes);

        JObject list = JObject.Parse(json);
        var properties = list.Properties();
        foreach (var prop in properties)
        {
            JObject infos = JObject.FromObject(list[prop.Name]);
            if (!((string)infos["name"]).Contains("~"))
            {
                string repoName = infos["name"].ToString().Replace(".git", "");
                string head = infos["HEAD"].ToString();
                Repo repo = new Repo
                {
                    Id = Guid.NewGuid(),
                    Url = prop.Name,
                    ReadmeIndex = 1,
                    Localisation = Repo.Loc.distant,
                    Name = repoName.Substring(repoName.LastIndexOf("/") + 1, repoName.Length - repoName.LastIndexOf("/") - 1),
                    Server = "gitblit",
                    ServerName = config.GetCurrentSource(),
                    Branch = head.Substring(head.LastIndexOf("/") + 1, head.Length - head.LastIndexOf("/") - 1)
                };
                repos.Add(repo);
            }
        }
        return repos;
    }

    public List<Repo> DevOpsRepoList()
    {
        List<Repo> repos = new List<Repo>();
        string json;
        try
        {
            json = Query("devops", config.GetServerUrl() + "_apis/git/repositories?api-version=5.0", config.GetCurrentSource());
            if (json != "")
            {
                JObject obj = JObject.Parse(json);
                JArray list = (JArray)obj["value"];
                foreach (JObject ob in list)
                {
                    Repo repo = new Repo
                    {
                        Id = Guid.NewGuid(),
                        Name = ob["name"].ToString(),
                        Url = ob["webUrl"].ToString(),
                        Localisation = Repo.Loc.distant,
                        ReadmeIndex = 1,
                        Server = "devops",
                        ServerName = config.GetCurrentSource()
                    };
                    try
                    {
                        string head = ob["defaultBranch"].ToString();
                        repo.Branch = head.Substring(head.LastIndexOf("/") + 1, head.Length - head.LastIndexOf("/") - 1);
                    } catch (Exception) { }
                    try
                    {
                        string result = Query("devops", config.GetServerUrl(repo.ServerName) + "_apis/git/repositories/" + repo.Name + "/commits?api-version=5.0", repo.ServerName);
                        JObject jsonCommit = JObject.Parse(result);
                        JArray valuesCommit = (JArray)jsonCommit["value"];
                        string commit = valuesCommit[0]["commitId"].ToString();

                        result = Query("devops", config.GetServerUrl(repo.ServerName) + "_apis/git/repositories/" + repo.Name + "/refs?filter=tags/&peelTags=true&api-version=5.0", repo.ServerName);
                        JObject jsonTag = JObject.Parse(result);
                        JArray values = (JArray)jsonTag["value"];
                        foreach (JObject obTag in values)
                        {
                            if (ob["objectId"] != null && ob["objectId"].ToString() == commit)
                            {
                                string tag = ob["name"].ToString();
                                repo.Tag = tag.Substring(tag.LastIndexOf("/") + 1, tag.Length - tag.LastIndexOf("/") - 1);
                                break;
                            }
                            else if (ob["peeledObjectId"] != null && ob["peeledObjectId"].ToString() == commit)
                            {
                                string tag = obTag["name"].ToString();
                                repo.Tag = tag.Substring(tag.LastIndexOf("/") + 1, tag.Length - tag.LastIndexOf("/") - 1);
                                break;
                            }
                        }
                    }
                    catch (Exception) { }
                    repos.Add(repo);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        return repos;
    }

    /*
    public List<string> GitHubRepoList()
    {
        List<string> repoList = new List<string>();
        string url = "https://api.github.com/users/" + config.GetUserName() + "/repos";
        Console.WriteLine(url);
        string json = "";
        try
        {
            using (var client = new WebClient())
            {
                json = client.DownloadString(url);
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        
        Console.WriteLine(json);
        if (json != "")
        {
            JArray list = JArray.Parse(json);
            foreach (JObject ob in list)
            {
                string name = ((string)ob["name"]).Replace(".git", "");
                repoList.Add(name.Substring(name.LastIndexOf("/") + 1, name.Length - name.LastIndexOf("/") - 1));
            }
        }
        return repoList;
    }
    */

    /**
     * Retourne la liste des dépôts du serveur
     */
    public List<Repo> GetRepoList()
    {
        List<Repo> repos = new List<Repo>();
        string currentType = config.GetCurrentType();
        if (currentType == "gitblit")
        {
            return GitBlitRepoList();
        }
        else if (currentType == "bitbucket")
        {
            return BitBucketRepoList();
        }
        else if (currentType == "devops")
        {
            return DevOpsRepoList();
        }
        /*
        else if (currentType == "github")
        {
            return GitHubRepoList();
        }
        */
        return repos;
    }

    /**
     * Liste les fichiers distants du dépôt d'un module et récupère la liste des #include
     */
    public List<string> GetModuleDep(Repo repo, string branch)
    {
        List<string> dep = new List<string>();
        string currentType = repo.Server;
        if(currentType == "gitblit")
        {
            string url = repo.Url.Remove(repo.Url.LastIndexOf("/"),1).Insert(repo.Url.LastIndexOf("/"),"%2f").Replace("/r/","/raw/") + "/" + branch;
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
                    return GetModuleDep(repo, "master");
                }
                HtmlNodeCollection collection = document.DocumentNode.SelectNodes("//a");
                foreach (HtmlNode link in collection)
                {
                    string target = link.Attributes["href"].Value;
                    if (target.Contains(".c") || target.Contains(".h"))
                    {
                        string serverUrl = config.GetServerUrl().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        dep.AddRange(GetIncludes(serverUrl + target, currentType, repo.ServerName));
                    }
                }
            } catch (Exception) { }
            
        }
        else if(currentType == "bitbucket")
        {
            string url = "https://api.bitbucket.org/2.0/repositories/" + config.GetUserName(repo.ServerName) + "/" + repo.Name + "/src/" + branch + "/";
            string json = "";
            try
            {
                json = Query("bitbucket",url, repo.ServerName);
            }
            catch (Exception)
            {
                if(branch != "master")
                    return GetModuleDep(repo, "master");
            }
            if(json != "")
            {
                JObject obj = JObject.Parse(json);
                JArray list = (JArray)obj["values"];
                foreach (JObject ob in list)
                {
                    if (((string)ob["path"]).Contains(".c") || ((string)ob["path"]).Contains(".h"))
                    {
                        string biturl = "https://bitbucket.org/" + config.GetUserName(repo.ServerName) + "/" + repo.Name + "/raw/" + branch + "/" + (string)ob["path"];
                        dep.AddRange(GetIncludes(biturl, currentType, repo.ServerName));
                    }
                }
            }
            else if(branch != "master")
            {
                return GetModuleDep(repo, "master");
            }
            
        }
        else if(currentType == "devops")
        {
            string url = config.GetServerUrl(repo.ServerName) + "_apis/git/repositories/" + repo.Name + "/items?recursionLevel=OneLevel&api-version=5.0";
            try
            {
                string res = Query("devops", url, repo.ServerName);
                JObject obj = JObject.Parse(res);
                JArray list = (JArray)obj["value"];
                foreach (JObject ob in list)
                {
                    if (ob["path"].ToString().Contains(".c") || ob["path"].ToString().Contains(".h"))
                    {
                        string devurl = config.GetServerUrl(repo.ServerName) + "_apis/git/repositories/" + repo.Name + "/items?path=" + ob["path"].ToString() + "&versionType=Branch&version=" + branch + "&includeContent=true&api-version=5.0";
                        try
                        {
                            if(Query("devops", devurl, repo.ServerName).Length == 0 && branch != "master")
                            {
                                return GetModuleDep(repo, "master");
                            }
                        }
                        catch (Exception)
                        {
                            if (branch != "master")
                                return GetModuleDep(repo, "master");
                        }
                        dep.AddRange(GetIncludes(devurl, currentType, repo.ServerName));
                    }
                }
            }
            catch (Exception)
            {
                return GetModuleDep(repo, "master");
            }
            
            
        }
        /*
        else if (currentType == "github")
        {
            string res;
            using (var client = new WebClient())
            {
                res = client.DownloadString("https://api.github.com/repos/" + config.GetUserName() + "/" + moduleText + "/contents/");
            }
            JArray list = JArray.Parse(res);
            foreach (JObject ob in list)
            {
                if (ob["path"].ToString().Contains(".c") || ob["path"].ToString().Contains(".h"))
                {
                    string url = ob["download_url"].ToString();
                    dep.AddRange(GetIncludes(url));
                }
            }
        }
        */
        return dep;
    }

    /**
     * Retourne la liste des #include d'un fichier distant
     */
    public List<string> GetIncludes(string url, string currentType, string serverName)
    {
        List<string> dep = new List<string>();
        string result = "";
        if (currentType == "gitblit")
        {
            using (var wc = new WebClient())
                result = wc.DownloadString(url);
        }
        else if (currentType == "bitbucket")
        {
            result = Query("bitbucket",url, serverName);
        }
        else if (currentType == "devops")
        {
            result = Query("devops",url, serverName);
        }
        /*
        else if(currentType == "github")
        {
            using (var client = new WebClient())
            {
                result = client.DownloadString(url);
            }
        }
        */
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

    public List<Repo> GetSubGitblit(string branch, string rep, Repo proj)
    {
        List<Repo> repos = new List<Repo>();
        string url = proj.Url.Replace("/r/","/raw/") + "/" + branch + "/.gitmodules";
        try
        {
            var client = new WebClient();
            using (var stream = client.OpenRead(url))
            using (var reader = new StreamReader(stream))
            {
                string Name = null, Server = null, Url = null, Path = null, Tag = null, Branch = null;
                Repo repo = new Repo();
                string ligne;
                while ((ligne = reader.ReadLine()) != null)
                {
                    if (ligne.Contains("branch!") && branch != "master")
                    {
                        if (affiche)
                        {
                            affiche = !ShowDialog("Ne plus afficher", "Le projet " + rep + " ne contient pas de branche " + config.GetBranchDev() + " ! \n\nRecherche sur la branche master\n ");
                        }
                        return GetSubmodList("master", rep, proj);
                    }
                    if (ligne.Contains("submodule"))
                    {
                        if (Name != null)
                        {
                            repo = new Repo
                            {
                                Id = Guid.NewGuid(),
                                Name = Name,
                                Server = Server,
                                Url = Url,
                                Path = Path,
                                Type = "module",
                                Tag = Tag,
                                Branch = Branch,
                                ReadmeIndex = 0,
                                Localisation = Repo.Loc.local,
                            };
                            repo.ServerName = GetServerName(repo);
                            repos.Add(repo);
                            Name = null; Server = null; Url = null; Path = null; Tag = null; Branch = null;
                        }   
                        repo = new Repo();
                    }
                    if (ligne.Contains("url"))
                    {
                        Name = ligne.Substring(ligne.LastIndexOf("/") + 1, ligne.Length - ligne.LastIndexOf("/") - 1).Replace(".git", "");
                        string URL = ligne.Replace("url = ", "");
                        Url = URL;
                        if (URL.Contains("azure") || URL.Contains("visualstudio"))
                            Server = "devops";
                        else if (URL.Contains("bitbucket"))
                            Server = "bitbucket";
                        else
                            Server = "gitblit";
                    }
                }
                if (Name != null)
                {
                    repo = new Repo
                    {
                        Id = Guid.NewGuid(),
                        Name = Name,
                        Server = Server,
                        Url = Url,
                        Path = Path,
                        Type = "module",
                        Tag = Tag,
                        Branch = Branch,
                        ReadmeIndex = 0,
                        Localisation = Repo.Loc.local,
                    };
                    repo.ServerName = GetServerName(repo);
                    repos.Add(repo);
                }
            }
        }
        catch (Exception) { }
        return repos;
    }

    public List<Repo> GetSubBitBucket(string branch, string rep, Repo proj)
    {
        List<Repo> repos = new List<Repo>();
        string result;
        try
        {
            result = Query("bitbucket", proj.Url + "/raw/" + branch + "/.gitmodules", proj.ServerName);
        }
        catch (Exception ex)
        {
            Console.WriteLine("GetSubmodError : " + ex.Message);
            if (branch != "master")
                return GetSubmodList("master", rep, proj);
            result = "null";
        }
        if (result.Length == 0 && branch != "master")
            return GetSubmodList("master", rep, proj);
        Repo repo = new Repo();
        string commit = "";
        using (StringReader reader = new StringReader(result))
        {
            string ligne;
            while ((ligne = reader.ReadLine()) != null)
            {
                if (ligne.Contains("dashboard") && branch != "master")
                {
                    if (affiche)
                    {
                        affiche = !ShowDialog("Ne plus afficher", "Le projet " + rep + " ne contient pas de branche " + config.GetBranchDev() + " ! \n\nRecherche sur la branche master\n ");
                    }
                    return GetSubmodList("master", rep, proj);
                }
                if (ligne.Contains("submodule"))
                {
                    if (repo.Name != null)
                    {
                        repo.ServerName = GetServerName(repo);
                        try
                        {
                            string tags = Query("bitbucket", "https://api.bitbucket.org/2.0/repositories/" + config.GetUserName() + "/" + repo.Name + "/refs/tags", config.GetCurrentSource());
                            Console.WriteLine("https://api.bitbucket.org/2.0/repositories/" + config.GetUserName() + "/" + repo.Name + "/refs/tags", config.GetCurrentSource());
                            Console.WriteLine(tags);
                            JObject json = JObject.Parse(tags);
                            JArray values = (JArray)json["values"];
                            foreach (JObject ob in values)
                            {
                                Console.WriteLine(ob["target"]["hash"] + " - " + commit);
                                if (ob["target"]["hash"].ToString() == commit)
                                {
                                    Console.WriteLine(ob["name"]);
                                    string tag = ob["name"].ToString();
                                    repo.Tag = tag;
                                    break;
                                }
                            }
                        }
                        catch (Exception) { Console.WriteLine("Can't get BitBucket Tags"); }
                        repo.Id = Guid.NewGuid();
                        repos.Add(repo);
                    }

                    repo = new Repo();
                    repo.ReadmeIndex = 0;
                    repo.Type = "module";
                    repo.Localisation = Repo.Loc.local;
                }
                if (ligne.Contains("url"))
                {
                    string name = ligne.Substring(ligne.LastIndexOf("/") + 1, ligne.Length - ligne.LastIndexOf("/") - 1).Replace(".git", "");
                    repo.Name = name;
                    string URL = ligne.Replace("url = ", "");
                    repo.Url = URL;
                    if (URL.Contains("azure") || URL.Contains("azure"))
                        repo.Server = "devops";
                    else if (URL.Contains("bitbucket"))
                        repo.Server = "bitbucket";
                    else
                        repo.Server = "gitblit";
                }
                if(ligne.Contains("path"))
                {
                    try
                    {
                        Console.WriteLine(proj.Url + "/raw/" + branch + "/" + ligne.Replace("path = ", "").Trim());
                        commit = Query("bitbucket", proj.Url.Replace("bitbucket.org", "api.bitbucket.org/2.0/repositories") + "/src/" + branch + "/" + ligne.Replace("path = ","").Trim(), proj.ServerName);
                        Console.WriteLine("Commit - " + commit);
                    }
                    catch (Exception) { Console.WriteLine("Error : " + proj.Url + "/raw/" + branch + "/" + ligne.Replace("path = ", "").Trim()); }
                }
            }
        }
        if (repo.Name != null)
        {
            repo.ServerName = GetServerName(repo);
            try
            {
                string tags = Query("bitbucket", "https://api.bitbucket.org/2.0/repositories/" + config.GetUserName() + "/" + repo.Name + "/refs/tags", config.GetCurrentSource());
                Console.WriteLine("https://api.bitbucket.org/2.0/repositories/" + config.GetUserName() + "/" + repo.Name + "/refs/tags", config.GetCurrentSource());
                JObject json = JObject.Parse(tags);
                JArray values = (JArray)json["values"];
                foreach (JObject ob in values)
                {
                    Console.WriteLine(ob["target"]["hash"] + " - " + commit);
                    if (ob["target"]["hash"].ToString() == commit)
                    {
                        Console.WriteLine(ob["name"]);
                        string tag = ob["name"].ToString();
                        repo.Tag = tag;
                        break;
                    }
                }
            }
            catch (Exception) { Console.WriteLine("Can't get BitBucket Tags"); }
            repo.Id = Guid.NewGuid();
            repos.Add(repo);
        }
        return repos;
    }

    public List<Repo> GetSubDevOps(string branch, string rep, Repo proj)
    {
        List<Repo> repos = new List<Repo>();
        string result;
        try
        {
            result = Query("devops", config.GetServerUrl() + "_apis/git/repositories/" + rep + "/items?path=.gitmodules&includeContent=true&api-version=5.0", config.GetCurrentSource());
        }
        catch (Exception ex)
        {
            Console.WriteLine("GetSubmodError : " + ex.Message);
            //if (branch != "master")
                //return GetSubmodList("master", rep, proj);
            result = "null";
        }
        //if (result.Length == 0 && branch != "master")
            //return GetSubmodList("master", rep, proj);

        using (StringReader reader = new StringReader(result))
        {
            string ligne;
            string commit = "";
            Repo repo = new Repo();
            while ((ligne = reader.ReadLine()) != null)
            {
                if (ligne.Contains("submodule"))
                {
                    if (repo.Name != null)
                    {
                        repo.ServerName = GetServerName(repo);
                        try
                        {
                            result = Query("devops", config.GetServerUrl(repo.ServerName) + "_apis/git/repositories/" + repo.Name + "/refs?filter=tags/&peelTags=true&api-version=5.0", repo.ServerName);
                            JObject json = JObject.Parse(result);
                            JArray values = (JArray)json["value"];
                            foreach (JObject ob in values)
                            {
                                if(ob["objectId"].ToString() == commit)
                                {
                                    string tag = ob["name"].ToString();
                                    repo.Tag = tag.Substring(tag.LastIndexOf("/") + 1, tag.Length - tag.LastIndexOf("/") - 1);
                                    break;
                                }
                                else if (ob["peeledObjectId"] != null && ob["peeledObjectId"].ToString() == commit)
                                {
                                    string tag = ob["name"].ToString();
                                    repo.Tag = tag.Substring(tag.LastIndexOf("/") + 1, tag.Length - tag.LastIndexOf("/") - 1);
                                    break;
                                }
                            }
                        } catch (Exception) { }
                        repo.Id = Guid.NewGuid();
                        repos.Add(repo);
                    }
                    repo = new Repo();
                    repo.ReadmeIndex = 0;
                    repo.Type = "module";
                    repo.Localisation = Repo.Loc.local;
                }
                if (ligne.Contains("url"))
                {
                    string name = ligne.Substring(ligne.LastIndexOf("/") + 1, ligne.Length - ligne.LastIndexOf("/") - 1).Replace(".git", "");
                    repo.Name = name;
                    string URL = ligne.Replace("url = ", "");
                    repo.Url = URL;
                    if (URL.Contains("azure") || URL.Contains("visualstudio"))
                        repo.Server = "devops";
                    else if (URL.Contains("bitbucket"))
                        repo.Server = "bitbucket";
                    else
                        repo.Server = "gitblit";
                }
                if(ligne.Contains("path"))
                {
                    string modPath = ligne.Replace("path = ", "").Trim();
                    try
                    {
                        commit = Query("devops", config.GetServerUrl() + "_apis/git/repositories/" + rep + "/items?path=" + modPath + "&includeContent=true&api-version=5.0", config.GetCurrentSource());
                    }
                    catch (Exception) { }
                    
                }
            }
            if (repo.Name != null)
            {
                repo.ServerName = GetServerName(repo);
                try
                {
                    result = Query("devops", config.GetServerUrl(repo.ServerName) + "_apis/git/repositories/" + repo.Name + "/refs?filter=tags/&peelTags=true&api-version=5.0", repo.ServerName);
                    JObject json = JObject.Parse(result);
                    JArray values = (JArray)json["value"];
                    foreach (JObject ob in values)
                    {
                        if (ob["peeledObjectId"].ToString() == commit)
                        {
                            string tag = ob["name"].ToString();
                            repo.Tag = tag.Substring(tag.LastIndexOf("/") + 1, tag.Length - tag.LastIndexOf("/") - 1);
                            break;
                        }
                    }
                } catch (Exception) { }
                repo.Id = Guid.NewGuid();
                repos.Add(repo);
            }
        }
        return repos;
    }
    /*
    public List<Repo> GetSubGitHub(string branch, string rep)
    {
        List<Repo> repos = new List<Repo>();
        string result;
        try
        {
            using (var client = new WebClient())
            {
                result = client.DownloadString("https://raw.githubusercontent.com/" + config.GetUserName() + "/" + rep + "/" + branch + "/.gitmodules");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("GetSubmodError : " + ex.Message);
            if (branch != "master")
                return GetSubmodList("master", rep);
            result = "null";
        }
        if (result.Length == 0 && branch != "master")
            return GetSubmodList("master", rep);

        using (StringReader reader = new StringReader(result))
        {
            string ligne;
            Repo repo = new Repo();
            while ((ligne = reader.ReadLine()) != null)
            {
                if (ligne.Contains("submodule"))
                {
                    if (repo.Name != null)
                        repos.Add(repo);
                    repo = new Repo();
                    repo.ReadmeIndex = 0;
                    repo.Type = "module";
                    repo.Localisation = Repo.Loc.local;
                }
                if (ligne.Contains("url"))
                {
                    string name = ligne.Substring(ligne.LastIndexOf("/") + 1, ligne.Length - ligne.LastIndexOf("/") - 1).Replace(".git", "");
                    repo.Name = name;
                    string URL = ligne.Replace("url = ", "");
                    repo.Url = URL;
                    //Uri myUri = new Uri(url);
                    //string host = myUri.Host;
                    //Console.WriteLine(host);
                    if (URL.Contains("azure") || URL.Contains("azure"))
                        repo.Server = "devops";
                    else if (URL.Contains("bitbucket"))
                        repo.Server = "bitbucket";
                    else
                        repo.Server = "gitblit";
                }
                if (ligne.Contains("branch"))
                {
                    string branche = ligne.Replace("branch = ", "");
                    repo.Branch = branche;
                }
            }
            if (repo.Name != null)
                repos.Add(repo);
        }
        return repos;
    }
    */

    /**
     * Retourne la liste des modules présents dans le fichier .gitmodules d'un projet distant
     */
    public List<Repo> GetSubmodList(string branch, string rep, Repo repo)
    {
        List<Repo> repos = new List<Repo>();
        string currentType = config.GetCurrentType();
        if(currentType == "gitblit")
        {
            return GetSubGitblit(branch, rep, repo);
        }
        else if (currentType == "bitbucket")
        {
            return GetSubBitBucket(branch, rep, repo);
        }
        else if (currentType == "devops")
        {
            return GetSubDevOps(branch, rep, repo);
        }
        /*
        else if(currentType == "github")
        {
            return GetSubGitHub(branch, rep);
        }
        */
        return repos;
    }

    /**
     * Retourne le nom du projet avec le dossier qui le contient
     */
    public string GetProjFullName(string path)
    {
        string line;
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
        string currentType = config.GetCurrentType();
        if(currentType == "gitblit")
        {
            //========================================= GITBLIT PATH =================================================//
            fullName = fullName.Substring(fullName.IndexOf(@"/_") + 1, fullName.Length - fullName.IndexOf(@"/_") - 1);
            //========================================================================================================//
        }
        else
        {
            fullName = fullName.Substring(fullName.LastIndexOf(@"/") + 1, fullName.Length - fullName.LastIndexOf(@"/") - 1);
        }
        return fullName;

    }

    public string GetProjURL(string path, string name, string type)
    {
        string line;
        string prev = "";
        string url = "";
        StreamReader file;
        try
        {
            file = new StreamReader(path + @"\.git\config");
        }
        catch (Exception) { return ""; }
        
        while ((line = file.ReadLine()) != null)
        {
            if(type == "module")
            {
                if (line.Contains("submodule") && line.Contains(name))
                    prev = line;
                if (prev.Contains("submodule") && line.Contains("url"))
                    return line.Replace("url = ", "");
            }
            else
            {
                if (line.Contains("remote"))
                    prev = line;
                if (prev.Contains("remote") && line.Contains("url"))
                    return line.Replace("url = ", "");
            }
            
        }
        return url;
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

    public List<string> GetNodes(TreeNodeCollection treeNode)
    {
        List<string> checkedList = new List<string>();

        foreach (TreeNode node in treeNode)
        {
            
            checkedList.Add(node.Text);

            if (node.Nodes.Count != 0)
            {
                checkedList.AddRange(GetNodes(node.Nodes));
            }
        }

        return checkedList;
    }

    /**
     * Retourne le contenu d'un fichier README dans une chaîne de caractères
     */
    public string GetMarkdown(Repo repo, string branch)
    {
        string md = "";
        string currentType = repo.Server;
        if (currentType == "gitblit")
        {
            try
            {
                int place = repo.Url.LastIndexOf("/");
                string url = repo.Url.Remove(place, 1).Insert(place, "%2F");
                using (var wc = new WebClient())
                    md = wc.DownloadString(url.Replace("/r/","/raw/") + "/" + branch + "/README.md");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed downloading README.md : " + ex.Message);
                if (branch != "master")
                    return GetMarkdown(repo, "master");
            }
            if (md.Contains("branch") && branch != "master")
            {
                return GetMarkdown(repo, "master");
            }
            byte[] bytes = Encoding.Default.GetBytes(md);
            md = Encoding.UTF8.GetString(bytes);
        }
        else if(currentType == "bitbucket")
        {
            string url = "https://bitbucket.org/" + config.GetUserName(repo.ServerName) + "/" + repo.Name + "/raw/" + branch + "/README.md";
            string query = "";
            try
            {
                query = Query("bitbucket", url, repo.ServerName);
            } catch (Exception)
            {
                if(branch != "master")
                    return GetMarkdown(repo, "master"); ;
            }
            
            if((query.Length == 0 || query.Contains("<html>")) && branch != "master")
            {
                return GetMarkdown(repo, "master");
            }
            else
            {
                return query;
            }
        }
        else if(currentType == "devops")
        {
            string result = Query("devops", config.GetServerUrl(repo.ServerName) + "_apis/git/repositories/" + repo.Name + "/items?path=README.md&includeContent=true&api-version=5.0", repo.ServerName);
            /*
            JObject obj = JObject.Parse(result);
            result = (string)obj["content"];
            */
            if (result.Length == 0 && branch != "master")
                return GetMarkdown(repo, "master");
            else
                return result;
        }
        else if(currentType == "github")
        {
            try
            {
                using (var wc = new WebClient())
                    md = wc.DownloadString("https://raw.githubusercontent.com/" + config.GetUserName(repo.ServerName) + "/" + repo.Name + "/" + branch + "/README.md");
            }
            catch (Exception ex)
            {
                if(branch != "master")
                    return GetMarkdown(repo, "master");
                Console.WriteLine(ex.Message);
            }
        }
        return md;
    }

    public string GetMarkdownLoc(string projPath)
    {
        string md = "";
        try
        {
            md = File.ReadAllText(projPath + @"\README.md");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Local Markdown Exception (path : " + projPath + ") : " + ex.Message);
        }
        return md;
    }

    /**
     * 
     */
    public bool SavePassword(string token, string name)
    {
        byte[] plaintext = Encoding.Default.GetBytes(token);
        byte[] entropy;
        byte[] ciphertext;
        if(config.GetPass() == "true")
        {
            using (Password formOptions = new Password())
            {
                formOptions.ShowDialog();
                try
                {
                    string result = formOptions.pass;
                    if (result.Length != 0)
                    {
                        entropy = Encoding.Default.GetBytes(result);
                        ciphertext = ProtectedData.Protect(plaintext, entropy, DataProtectionScope.CurrentUser);
                        File.WriteAllBytes(config.GetAppData() + ".cred" + name, ciphertext);
                        return true;
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        } else
        {
            using (NewPassword formOptions = new NewPassword())
            {
                formOptions.ShowDialog();
                try
                {
                    string result = formOptions.pass;
                    if (result.Length != 0)
                    {
                        entropy = Encoding.Default.GetBytes(result);
                        ciphertext = ProtectedData.Protect(plaintext, entropy, DataProtectionScope.CurrentUser);
                        File.WriteAllBytes(config.GetAppData() + ".cred" + name, ciphertext);
                        config.SetPass("true");
                        return true;
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
        
        return false;
    }

    public int GetIndex(TreeNode node)
    {
        /// source : David Fletcher

        int returnValue = 0;

        // Always make a way to exit the recursion.
        if (node.Index == 0 && node.Parent == null)
            return returnValue;

        // Now, count every node.
        returnValue = 1;

        // If I have siblings higher in the index, then count them and their decendants.
        if (node.Index > 0)
        {
            TreeNode previousSibling = node.PrevNode;
            while (previousSibling != null)
            {
                returnValue += GetDecendantCount(previousSibling);
                previousSibling = previousSibling.PrevNode;
            }
        }

        if (node.Parent == null)
            return returnValue;
        else
            return returnValue + GetIndex(node.Parent);
    }

    public int GetDecendantCount(TreeNode node)
    {
        int returnValue = 0;

        // If the node is not the root node, then we want to count it.
        if (node.Index != 0 || node.Parent != null)
            returnValue = 1;

        // Always make a way to exit a recursive function.
        if (node.Nodes.Count == 0)
            return returnValue;

        foreach (TreeNode childNode in node.Nodes)
        {
            returnValue += GetDecendantCount(childNode);
        }
        return returnValue;
    }

    public List<string> GetSmartGitList()
    {
        List<string> smartList = new List<string>();

        XDocument xml = XDocument.Load(config.GetSmartGitRepo());

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
                smartList.Add(chemin);
            }
        }

        return smartList;
    }

    public List<string> GetSourceTreetList()
    {
        List<string> sourceList = new List<string>();

        XDocument xml = XDocument.Load(config.GetSourceTreeRepo());

        foreach(XElement elem in xml.Root.Elements())
        {
            sourceList.Add(elem.Value.ToString());
        }

        return sourceList;
    }

    public string GetBranch(string path, string head)
    {
        if (head.Contains("ref"))
        {
            return head.Substring(head.LastIndexOf("/") + 1, head.Length - head.LastIndexOf("/") - 1);
        }
        else
        {
            foreach (var file in new DirectoryInfo(Path.Combine(path,@"refs\heads")).GetFiles())
            {
                string ligne;
                StreamReader pack = new StreamReader(file.FullName);
                while ((ligne = pack.ReadLine()) != null)
                {
                    if (ligne.Contains(head))
                    {
                        return file.Name;
                    }
                }
                pack.Close();
            }
        }
        return null;
    }

    public string GetTag(string path, string head)
    {
        foreach (var file in new DirectoryInfo(Path.Combine(path, @"refs\tags")).GetFiles())
        {
            string ligne;
            StreamReader pack = new StreamReader(file.FullName);
            while ((ligne = pack.ReadLine()) != null)
            {
                if (ligne.Contains(head))
                {
                    return file.Name;
                }
            }
            pack.Close();
        }

        if(File.Exists(Path.Combine(path,"packed-refs")))
        {
            string ligne;
            StreamReader pack = new StreamReader(Path.Combine(path, "packed-refs"));
            while ((ligne = pack.ReadLine()) != null)
            {
                if (ligne.Contains(head) && ligne.Contains("tags"))
                {
                    return ligne.Substring(ligne.LastIndexOf("/") + 1, ligne.Length - ligne.LastIndexOf("/") - 1);
                }
            }
            pack.Close();
        }

        return null;
    }

    public string GetServerName(Repo repo)
    {
        if (repo.Name != null)
        {
            List<string> allNames = config.GetAllNames();
            List<string> allServ = config.GetAllTypes();
            List<string> alluniques = config.GetAllUniques();
            int i = 0;
            foreach (string unique in alluniques)
            {
                if (repo.Url.Contains(unique) && repo.Server == allServ.ElementAt(i))
                {
                    return allNames.ElementAt(i);
                }

                i++;
            }
        }
        return null;
    }

    public string GetSubmodGitPath(Repo repo, string parentPath)
    {
        string line;
        StreamReader file = new StreamReader(Path.Combine(parentPath,".gitmodules"));
        while ((line = file.ReadLine()) != null)
        {
            if (line.Contains("submodule ") && line.Contains(repo.Name))
            {
                string result = line.Replace("[submodule \"", "").Replace("\"]","");
                Console.WriteLine("res : " + result);
                file.Close();
                return result;
            }
        }
        file.Close();
        return null;
    }

}
