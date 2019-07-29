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
        public string ServerName { get; set; }
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
            //if (Server == "gitblit" && repo.Server == Server && Name.ToLower().Contains(repo.Name.ToLower()))
                //return true;
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
            Config config = new Config();
            List<string> allNames = config.GetAllNames();
            List<string> allServ = config.GetAllTypes();
            List<string> alluniques = config.GetAllUniques();
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

            string head = "";
            StreamReader headFile = new StreamReader(System.IO.Path.Combine(path, @".git\HEAD"));
            while ((line = headFile.ReadLine()) != null)
            {
                head = line;
            }
            headFile.Close();

            Functions functions = new Functions();
            Branch = functions.GetBranch(System.IO.Path.Combine(path,@".git\"), head);
            Tag = functions.GetTag(System.IO.Path.Combine(path, @".git\"), head);

            int i = 0;
            foreach (string unique in alluniques)
            {
                if (Url != null && Url.Contains(unique) && Server == allServ.ElementAt(i))
                {
                    ServerName = allNames.ElementAt(i);
                    break;
                }
                i++;
            }
            return this;
        }
    }
}
