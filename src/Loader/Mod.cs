using System;
using System.Reflection;
using Harmony;

namespace Loader
{
    public class Mod
    {
        public string Name;
        public string Description;
        public string Creator;
        public Version Version;

        public Assembly[] Assemblies;
        public HarmonyInstance Harmony;
    }
}