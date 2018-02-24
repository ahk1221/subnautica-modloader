using System;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using static System.Console;

namespace Installer
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            string root = Environment.CurrentDirectory;

            string tempAssemblyCSharpPath = Path.Combine(root, "Assembly-CSharp-temp.dll");
            string backupAssemblyCSharpPath = Path.Combine(root, "Assembly-CSharp-backup.dll");
            
            string assemblyCSharpPath = Path.Combine(root, "Assembly-CSharp.dll");
            string loaderPath = Path.Combine(root, "Loader.dll");

            try
            {
                if (Path.GetFileName(root) != "Managed") throw new DirectoryNotFoundException("Installer.exe has not been run from the managed folder");

                if (!File.Exists(assemblyCSharpPath) || !File.Exists(loaderPath))
                    throw new FileNotFoundException("Cannot find the right files in the current folder");

                using (var assemblyCsharp = ModuleDefMD.Load(assemblyCSharpPath))
                using (var modLoader = ModuleDefMD.Load(loaderPath))
                {
                    TypeDef main = modLoader.GetTypes().First(x => x.Name == "Main");

                    IMethod mainInitialize = assemblyCsharp.Import(main.FindMethod("Initialize"));
                    MethodDef constructor = assemblyCsharp.GlobalType.FindOrCreateStaticConstructor();
                    constructor.Body.Instructions.Insert(0, OpCodes.Call.ToInstruction(mainInitialize));

                    assemblyCsharp.Write(tempAssemblyCSharpPath);
                }

                File.Move(assemblyCSharpPath, backupAssemblyCSharpPath);
                File.Move(tempAssemblyCSharpPath, assemblyCSharpPath);

                WriteLine("Installation was successful");
            }
            catch (Exception ex)
            {
                WriteLine("An exception has occured:");
                WriteLine(ex);
            }
            finally
            {
                try
                {
                    if (File.Exists(tempAssemblyCSharpPath)) File.Delete(tempAssemblyCSharpPath);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    WriteLine($"Unable to delete temporary file \"{tempAssemblyCSharpPath}\"");
                }

                if (args.Length == 0 || args[0] != "q")
                {
                    WriteLine("Press any key to exit...");
                    ReadKey();
                }
            }
        }
    }
}