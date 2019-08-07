//============================================================================//
//                              REPO CLASS                                    //
//                                                                            //
// - Class to store informations of a repository                              //
//============================================================================//

using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace module_manager
{
    public class Repo
    {
        public enum Loc : int { local = 0, distant = 1, both = -1 };

        public Guid Id { get; set; }        // unique id
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
        
        /// <summary>
        /// Verifie si 2 Repo pointent sur le même dépôt (indépendamment de l'emplacement local ou distant)
        /// </summary>
        public bool Equal(Repo repo)
        {
            if (repo.Name.ToLower() == Name.ToLower() && repo.ServerName == ServerName)
                return true;
            return false;
        }

        /// <summary>
        /// Verifie si un Repo est présent dans une liste de Repo
        /// </summary>
        public bool IsInList(List<Repo> repos)
        {
            return repos.Any(mod => mod.Equal(this));
        }

        /// <summary>
        /// Initialise un Repo local depuis son chemin d'acces
        /// </summary>
        public Repo Init(string path)
        {
            Id = Guid.NewGuid();
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
                Name = path.Substring(path.LastIndexOf(@"\") + 1, path.Length - path.LastIndexOf(@"\") - 1);
            conf.Close();
            using (var reposit = new Repository(path))
            {
                foreach (var tags in reposit.Tags)
                {
                    if (tags.PeeledTarget.Id == reposit.Head.Tip.Id)
                        Tag = tags.FriendlyName;
                }
                Branch = reposit.Head.FriendlyName;
            }
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

        public Repo Init(string Name, string Server, string Url, string Path, string Type, string Tag, string Branch, int ReadmeIndex, Loc Localisation)
        {
            return new Repo
            {
                Id = Guid.NewGuid(),
                Name = Name,
                Server = Server,
                Url = Url,
                Path = Path,
                Type = Type,
                Tag = Tag,
                Branch = Branch,
                ReadmeIndex = ReadmeIndex,
                Localisation = Localisation
            };
        }

    }
}
