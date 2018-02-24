using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace Loader
{
    internal static class CommandManager
    {
        private static bool registered = false;
        private static List<CommandInfo> commands = new List<CommandInfo>();
        
        public static void AddCommand(MethodInfo info, string command, bool caseSensitive, bool combineArgs)
        {
            var parameters = info.GetParameters();
            var parameterTypes = new Type[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                parameterTypes[i] = parameters[i].ParameterType;
            }
            
            var proxy = new DynamicMethod($"OnConsoleCommand_{command}",
                MethodAttributes.Static,
                CallingConventions.Any,
                typeof(void),
                parameterTypes,
                typeof(CommandHost),
                true);
            
            var il = proxy.GetILGenerator();
            for (int i = 0; i < parameters.Length; i++) il.Ldarg(i);
            il.Emit(OpCodes.Call, info);
            il.Emit(OpCodes.Ret);
            
            commands.Add(new CommandInfo()
            {
                Name = command,
                CaseSensitive = caseSensitive,
                CombineArgs = combineArgs
            });
        }

        public static void RegisterCommands()
        {
            if (registered) throw new Exception("Commands already registered!");
            registered = true;
            
            var hostObject = new GameObject();
            UnityEngine.Object.DontDestroyOnLoad(hostObject);
            var hostComponent = hostObject.AddComponent<CommandHost>();
            
            foreach (var c in commands)
            {
                DevConsole.RegisterConsoleCommand(hostComponent, c.Name, c.CaseSensitive, c.CombineArgs);
            }

            commands = null;
        }
        
        private static void Ldarg(this ILGenerator il, int i)
        {
            switch (i)
            {
                case 0:
                    il.Emit(OpCodes.Ldarg_0);
                    return;
                case 1:
                    il.Emit(OpCodes.Ldarg_1);
                    return;
                case 2:
                    il.Emit(OpCodes.Ldarg_2);
                    return;
                case 3:
                    il.Emit(OpCodes.Ldarg_3);
                    return;
                case int b when i >= byte.MinValue && i <= byte.MaxValue:
                    il.Emit(OpCodes.Ldarg_S, (byte) b);
                    return;
                default:
                    il.Emit(OpCodes.Ldarg, i);
                    return;
            }
        }

        private struct CommandInfo
        {
            public string Name;
            public bool CaseSensitive;
            public bool CombineArgs; 
        }
        
        private sealed class CommandHost : MonoBehaviour
        {
            // Dynamic methods will be generated here
        }
    }
}