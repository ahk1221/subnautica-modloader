using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Harmony;
using LitJson;

namespace Loader
{
    public static class Main
    {
        public static readonly DirectoryInfo ModsFolder = new DirectoryInfo("./Subnautica_Data/Mods");

        public static HarmonyInstance Harmony;
        
        public static List<Mod> Mods = new List<Mod>();
        public static List<IScript> ActiveScripts = new List<IScript>();
        public static Dictionary<ScriptAttribute, Type> Scripts = new Dictionary<ScriptAttribute, Type>(); // The Mod class should probably own this
        
        public static void Initialize()
        {
            Harmony = HarmonyInstance.Create("ModLoader");
            Harmony.PatchAll(Assembly.GetExecutingAssembly());
            
            Load();
        }

        public static void Load()
        {
            foreach (var folder in ModsFolder.GetDirectories("*", SearchOption.TopDirectoryOnly))
            {
                string config = Path.Combine(folder.FullName, "config.cfg");
                if (File.Exists(config))
                {
                    string jsonText = File.ReadAllText(config);
                    var json = JsonMapper.ToObject(jsonText);

                    var mod = new Mod()
                    {
                        Name = (string)json["name"],
                        Description = (string)json["description"],
                        Creator = (string)json["creator"],
                        Version = new Version((string)json["version"])
                    };
                    
                    mod.Harmony = HarmonyInstance.Create(mod.Name);

                    foreach (var script in folder.GetFiles("*.dll", SearchOption.AllDirectories))
                    {
                        var assembly = Assembly.LoadFrom(script.FullName);
                        mod.Harmony.PatchAll(assembly);

                        foreach (var type in assembly.GetTypes())
                        {
                            if (type.IsSubclassOf(typeof(IScript)))
                            {
                                var attribute = (ScriptAttribute) type
                                    .GetCustomAttributes(typeof(ScriptAttribute), false)
                                    .FirstOrDefault();
                                
                                if (attribute != null)
                                {
                                    if (attribute.Start == ScriptStartup.Always)
                                    {
                                        var initializedScript = (IScript) Activator.CreateInstance(type);
                                        initializedScript.Start(); // Not the best way cause it's not guaranteed all mods are loaded when the script is started
                                    }
                                    else
                                    {
                                        Scripts.Add(attribute, type);
                                    }
                                }
                            }
                        }
                    }
                    
                    Mods.Add(mod);
                }
            }
        }

        private static void StartScripts(ScriptStartup start) // Change this, it doesn't allow for scripts to run in multiple scenes
        {
            foreach (var script in ActiveScripts)
            {
                script.Stop();
            }
            
            ActiveScripts.Clear();
            
            foreach (var pair in Scripts)
            {
                if (pair.Key.Start == start)
                {
                    var script = (IScript) Activator.CreateInstance(pair.Value);
                    ActiveScripts.Add(script);
                    script.Start();
                }
            }
        }
    }
}