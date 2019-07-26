using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace module_manager
{
    public class Repo
    {
        public enum Loc : int { local = 0, distant = 1, both = -1 };

        public string Name { get; set; }    // last part of the URL
        public string Server { get; set; }  // <gitblit|bitbucket|devops>
        public string Url { get; set; }
        public string Path { get; set; }    // local path or null
        public string Type { get; set; }    // <project|module>
        public string Tag { get; set; }
        public string Branch { get; set; }
        public List<Repo> Modules { get; set; }
        public int ReadmeIndex { get; set; }    // 0 : local ; 1 : distant
        public Loc Localisation { get; set; }   // 0 : local ; 1 : distant ; -1 : both
        
        public bool Equal(Repo repo)
        {
            if (Server == "gitblit" && repo.Server == Server && Name.ToLower().Contains(repo.Name.ToLower()))
                return true;
            if (repo.Name.ToLower() == Name.ToLower() && repo.Server == Server)
                return true;
            return false;
        }

        public bool IsInList(List<Repo> repos)
        {
            return repos.Any(mod => mod.Equal(this));
        }

        public Repo Init(string path)
        {
            Modules = new List<Repo>();
            if (File.Exists(System.IO.Path.Combine(path, ".gitmodules")))
                Type = "project";
            else
                Type = "module";

            Path = path;
            ReadmeIndex = 0;
            Localisation = Loc.local;
            string line, prev = "";
            StreamReader conf = new StreamReader(System.IO.Path.Combine(path, @".git\config"));
            while ((line = conf.ReadLine()) != null)
            {
                if (line.Contains("url") && prev.Contains("remote"))
                {
                    string url = line.Replace("url = ", "").Trim();
                    Url = url;
                    if (url.Contains("azure") || url.Contains("visualstudio"))
                        Server = "devops";
                    else if (url.Contains("bitbucket"))
                        Server = "bitbucket";
                    else
                        Server = "gitblit";
                    string name = url.Substring(url.LastIndexOf("/") + 1, url.Length - url.LastIndexOf("/") - 1).Replace(".git","");
                    Name = name;
                }
                prev = line;
            }
            if(Name == null)
            {
                Name = path.Substring(path.LastIndexOf(@"\") + 1, path.Length - path.LastIndexOf(@"\") - 1);
            }
            conf.Close();

            StreamReader head = new StreamReader(System.IO.Path.Combine(path, @".git\HEAD"));
            while ((line = head.ReadLine()) != null)
            {
                if (line.Contains("ref"))
                {
                    Branch = line.Substring(line.LastIndexOf("/") + 1, line.Length - line.LastIndexOf("/") - 1);
                }
                else if(File.Exists(System.IO.Path.Combine(path, @".git\packed-refs")))
                {
                    string ligne;
                    StreamReader pack = new StreamReader(System.IO.Path.Combine(path, @".git\packed-refs"));
                    while ((ligne = pack.ReadLine()) != null)
                    {
                        if(ligne.Contains(line) && ligne.Contains("remote"))
                        {
                            Branch = ligne.Substring(ligne.LastIndexOf("/") + 1, ligne.Length - ligne.LastIndexOf("/") - 1);
                        }
                        if (ligne.Contains(line) && ligne.Contains("tag"))
                        {
                            Tag = ligne.Substring(ligne.LastIndexOf("/") + 1, ligne.Length - ligne.LastIndexOf("/") - 1);
                        }
                    }
                    pack.Close();
                }
            }
            head.Close();

            return this;
        }
    }
}
