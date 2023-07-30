using Atrufulgium.FrontTick.Compiler.Datapack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// Note: In order to change the debug command line params, select the
// `Compiler` project, and go to Debug > Compiler Debug Options.
// Alternatively, it can be found in Properties > Debug.
// (Future me will forget, so this'll save me a search.)
// In my case, the commandline arguments are currently
//    -c ./IngameTests -m ./MCMirror -w FrontTick
// with working directory the parent directory of the projects.
namespace Atrufulgium.FrontTick.Compiler
{
    internal class Program {
        // Everything here is very temporary.
        static int Main(string[] args) {
            try {
                if (args.Length == 0)
                    return InteractiveMain();
                else
                    return CommandlineArgMain(args);
            } catch (Exception e) {
                Console.WriteLine("Uncaught exception:");
                Console.WriteLine(e);
                return 1;
            }
        }

        static int InteractiveMain() {
            throw new NotImplementedException();
        }

        static int CommandlineArgMain(string[] args) {
            // Just the quick hacky way while there's still few args
            string inputDirectory = null;
            string mcMirrorDirectory = null;
            string outputWorld = null;
            string manespace = null;
            for (int i = 0; i < args.Length; i++) {
                switch (args[i].ToLowerInvariant()) {
                    case "-c":
                    case "--code":
                        i++;
                        inputDirectory = args[i];
                        break;
                    case "-w":
                    case "--world":
                        i++;
                        outputWorld = args[i];
                        break;
                    case "-n":
                    case "--namespace":
                    case "--manespace": // I'm too used to it
                        i++;
                        manespace = args[i];
                        break;
                    case "-m":
                    case "--mcmirror":
                        i++;
                        mcMirrorDirectory = args[i];
                        break;
                    default:
                        Console.WriteLine($"Unknown option {args[i]}, use -c, -m, -w, or -n, self.");
                        return 1;
                }
            }
            if (inputDirectory == null) {
                Console.WriteLine("Did not specify compilation directory (with `-c`); assuming current directory");
                inputDirectory = Environment.CurrentDirectory;
            }
            if (outputWorld == null) {
                Console.WriteLine("Did not specify minecraft world name to output to (with `-w`)");
                return 1;
            }
            if (manespace == null) {
                Console.WriteLine("Did not specify compilation namespace (with `-n`); assuming `compiled`");
                manespace = "compiled";
            }
            if (mcMirrorDirectory == null) {
                Console.WriteLine("Did not specify the MCMirror directory (with `-m`); assuming it is part of the input directory");
            }
            return Compile(inputDirectory, mcMirrorDirectory, outputWorld, manespace);
        }

        static int Compile(string inputDirectory, string mcMirrorDirectory, string outputWorld, string manespace) {
            Console.WriteLine("Compiling with the following settings:");
            Console.WriteLine($"    Code directory:         {inputDirectory}");
            Console.WriteLine($"    MCMirror directory:     {mcMirrorDirectory}");
            Console.WriteLine($"    Output world name:      {outputWorld}");
            Console.WriteLine($"    Datapack namespace:     {manespace}");
            try {
                IEnumerable<(string code, string path)> code = FolderToContainingCode.GetCode(inputDirectory);
                if (mcMirrorDirectory != null)
                    code = code.Concat(FolderToContainingCode.GetCode(mcMirrorDirectory));
                Compiler compiler = new(manespace: manespace);
                // Compilation phases here yada yada
                compiler.Compile(code);
                if (compiler.WarningDiagnostics.Count > 0) {
                    Console.WriteLine("Compilation warnings:");
                    foreach (var diagnostic in compiler.WarningDiagnostics)
                        Console.WriteLine(diagnostic);
                }
                if (compiler.CompilationFailed) {
                    Console.WriteLine("Compilation failed! Diagnostics:");
                    foreach (var diagnostic in compiler.ErrorDiagnostics)
                        Console.WriteLine(diagnostic);
                    return 1;
                }
                FullDatapack datapack = compiler.CompiledDatapack;
                string datapackPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var slash = Path.DirectorySeparatorChar;
                datapackPath += $"{slash}.minecraft{slash}saves{slash}{outputWorld}{slash}datapacks{slash}{manespace}";
                datapack.WriteToFilesystem(datapackPath);
                Console.WriteLine("\nCompilation succesful!");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Don't forget to /reload in-game!");
                Console.ResetColor();
            } catch (IOException e) {
                Console.WriteLine("\n\n\nIO trouble ffs:\n");
                Console.WriteLine(e);
                return 1;
            }
            return 0;
        }
    }
}
