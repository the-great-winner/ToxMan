using Microsoft.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using SharpTox.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;

namespace ToxMan
{

    internal class ToxUtils
    {
        public static List<ToxNodeFound> UpdatedNodes { get; private set; }
        public static string DirExe =
            Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        public static string DirData =
            Path.Combine(
            (Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)),
            Assembly.GetEntryAssembly().GetName().Name);
        public static string PathToxNodes =>
           Path.Combine(DirData, "ToxNodes.json");
        public static string PathToxFriends =>
           Path.Combine(DirData, "Friends.json");
        public static string DirChats =>
           Path.Combine(DirData, "Chats");

        static ToxUtils()
        {
            CreateDirData();
            try { Directory.CreateDirectory(DirChats); } catch { }
        }

        internal static void LoadMyself()
        {
          Me =  Load<ToxManPerson>(PathProfile);
            if (Me == null || Me?.Secret == null)
                Me = new ToxManPerson();
        }

        private static void CreateDirData()
        {
            if (App.Args.Length > 0)
            {
                CommandLineApplication app = new CommandLineApplication(false);
                app.Execute(App.Args);
             //   app.Arguments;//.Where(x => x.Name == "datadir").
            }
            //pr
          

            try { Directory.CreateDirectory(DirData); return; }
            catch { }
            DirData = Path.Combine(DirExe, "Config");
            try { Directory.CreateDirectory(DirData); return; }
            catch { }
        }

        public static ToxManFriends Friends { get; private set; }
        public static ToxManPerson Me { get; private set; }
        public static string PathProfile => Path.Combine(DirData, ("Myself.json"));
         

        internal static ToxNode[] UpdateNodeList()
        {
            var url = "https://nodes.tox.chat/json";

            try
            {
                using (WebClient wc = new WebClient())
                {
                    string res = wc.DownloadString(url);
                    return ProcessFoundToxNodes(res);
                }
            }
            catch
            {
                ProcessFoundToxNodes(LoadToxNodes());
            }

            return null;
        }

        private static string LoadToxNodes()
        {
            try
            {
                return File.ReadAllText(PathToxNodes);
            }
            catch { }
            return null;
        }

        public static T Load<T>(string file)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(
                    File.ReadAllText( file  ));
            }
            catch { }
            return default (T);
        }

        private static ToxNode[] ProcessFoundToxNodes(string res, bool compare = true)
        {
            var t0 = new List<ToxNodeFound>();
            var tn = JsonConvert.DeserializeAnonymousType(res, t0);
            var t = new { nodes = tn };
            if (t.nodes.Count > 0)
            {
                if (compare)
                    try
                    {
                        var loaded = LoadToxNodes();
                        var tL = JsonConvert.DeserializeAnonymousType(loaded, t0);
                    }
                    catch
                    {
                    }

                UpdatedNodes = t.nodes;
                Save(t, PathToxNodes);
                return UpdatedNodes
                    .Select(x => NewNode(x))
                    .Where(y => y != null)
                    .ToArray();
            }
            return null;
        }

        private static ToxNode NewNode(ToxNodeFound x)
        {
            try
            {
                ToxNode y = new ToxNode(x.ipv4 ?? x.ipv6, x.port,
                                   new ToxKey(ToxKeyType.Public, x.public_key));
                return y;
            }
            catch { }
            return null;
        }

        internal static void LoadFriendIds()
        {
            try
            {
                Friends = JsonConvert.DeserializeObject<ToxManFriends>(File.ReadAllText(PathToxFriends));
                
                if (Friends == null)
                    throw new NullReferenceException();

                Friends.All = Friends.All.Where(x => !string.IsNullOrEmpty(x.ToxID)).ToList();
            }
            catch(Exception e)
            {
                Friends = new ToxManFriends { All = new List<Friend>() };
                Save(Friends, PathToxFriends);
            }
        }

        internal static void LogChat(string v, string g)
        {
            File.AppendAllLines(ChatPath(v), new[] { g } );
        }

        private static string ChatPath(string v)
        {
            return Path.Combine(DirChats, v + ".log");
        }

        internal static void SaveAllCfgFiles()
        {
            Save(Me, PathProfile);
            Save(Friends, PathToxFriends);
            Save(UpdatedNodes, PathToxNodes);
        }

        private static void Save(object what, string where)
        {
            try
            {
                var ser = JsonConvert.SerializeObject(what, Formatting.Indented);
                var wb = where + ".bak";
                if (File.Exists(where))
                {
                    if (!File.Exists(wb) || Inequal(ser, wb))
                        File.Copy(where, wb, true);
                }
                File.WriteAllText(where, ser);
            }
            catch (Exception ex)
            {

            }
        }

        private static bool Inequal(string contentsToCompare, string pathToLoad)
        {
            try { return (File.ReadAllText(pathToLoad) == contentsToCompare); } catch { }
            return false;
        }

        internal static void AddFriend(ToxKey newKey)
        {           
          Friends.All.Add(new Friend { ToxID = newKey.GetString() });
        }

        internal static bool FriendAdded(ToxKey newKey)
        { 
            var s = newKey.GetString();
            return (Friends.All.Where(x => x.ToxID == s).Count() > 0);
        }

        internal static string ChatHistory(string v)
        {
            try { return File.ReadAllText(ChatPath(v)); }
            catch { }
            return "";
        }
    }

    public class ToxManPerson
    {
        public string Secret { get; set; }
#if DEBUG
            = "9a71a67e24a6ab1205a027c538f075709e5a2b88075f58e4ddaa0234c7aa4bb9";
#endif
        public string Name { get; set; } = "ToxMan User";
        public string StatusMsg { get;  set; } = "Luxi does it";
    }

    public class ToxManFriends
    {
       public  List<Friend> All { set; get; }
    }

    public class Friend
    {
       public string ToxID { get; set; }
       public string LocalGroupName { get; set; }
       public List<string> Tags { get; set; }
    }

    public class ToxNodeFound
    {
        public string ipv4 { set; get; }
        public string ipv6 { set; get; }
        public int port { set; get; }
        public int last_ping { set; get; }
        public string public_key { set; get; }
        public string location { set; get; }
        public string maintainer { set; get; }
        public string motd { set; get; }
        public string version { set; get; }
        public bool status_tcp { set; get; }
        public bool status_udp { set; get; }
        public int[] tcp_ports { set; get; }
    }
}