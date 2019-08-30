﻿using NDesk.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace OTAPI.Patcher
{
    public class Program
    {
        static Engine.Patcher patcher;
        static OptionSet options;

        public static void Main(string[] args)
        {
            string sourceAsm = null;
            string modificationGlob = null;
            string outputPath = null;
            var mergeInputs = new List<string>();
            string mergeOutput = null;

            Console.WriteLine("Open Terraria API v2.0");

            if (args.Length == 0)
            {
                var relativePath = Regex.IsMatch(Environment.CurrentDirectory, "(bin|obj).(Debug|Release)") ? "../../../" : "./";
                args = new[]
                {
                    $"-pre-merge-in={relativePath}wrap/TerrariaServer/TerrariaServer.exe",
#if V1353
                    $"-pre-merge-in={relativePath}wrap/TerrariaServer/ReLogic.dll",
#endif
                    $"-pre-merge-out={relativePath}TerrariaServer.dll",
                    $"-in={relativePath}TerrariaServer.dll",
#if DEBUG
                    $"-mod={relativePath}OTAPI.Modifications/OTAPI.Modifications.*/bin/Debug/OTAPI.*.dll",
#else
                    $"-mod={relativePath}OTAPI.Modifications/OTAPI.Modifications.*/bin/Release/OTAPI.*.dll",
#endif
                    $"-o={relativePath}OTAPI.dll"
                };
            }

            options = new OptionSet();
            options.Add("in=|source=", "specifies the source assembly to patch",
                op => sourceAsm = op);
            options.Add("mod=|modifications=", "Glob specifying the path to modification assemblies that will run against the target assembly.",
                op => modificationGlob = op);
            options.Add("o=|output=", "Specifies the output assembly that has had all modifications applied.",
                op => outputPath = op);
            options.Add("pre-merge-in=", "Specifies an assembly to be combined before any modifications are applied",
                op => mergeInputs.Add(op));
            options.Add("pre-merge-out=", "Specifies the output file of combined assemblies before any modifications are applied",
                op => mergeOutput = op);

            options.Parse(args);

            if (string.IsNullOrEmpty(sourceAsm) || string.IsNullOrEmpty(modificationGlob))
            {
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            if (mergeInputs.Count > 0)
            {
                var extractedReferences = new List<String>();
                // extract embedded resources so that ilrepack can find additional references
                foreach (var input in mergeInputs)
                {
                    var info = new FileInfo(input);
                    if (info.Exists)
                    {
                        var ext = new Engine.Framework.EmbeddedResourceExtractor(input)
                        {
                            Extensions = new[] { ".dll", ".exe" }
                        };
                        extractedReferences.AddRange(ext.Extract());
                    }
                }
                // rename resources to match their assembly names
                foreach (var input in extractedReferences)
                {
                    Console.WriteLine(input);
                    var dest = Path.Combine(Path.GetDirectoryName(input), AssemblyName.GetAssemblyName(input).Name + Path.GetExtension(input));
                    Console.WriteLine(dest);
                    if (File.Exists(dest))
                        File.Delete(dest);

                    File.Move(input, dest);
                }

                var roptions = new ILRepacking.RepackOptions()
                {
                    //Get the list of input assemblies for merging
                    InputAssemblies = mergeInputs.ToArray(),

                    OutputFile = mergeOutput,
                    TargetKind = ILRepacking.ILRepack.Kind.Dll,

                    //Setup where ILRepack can look for assemblies
                    SearchDirectories = mergeInputs
                        .Select(x => Path.GetDirectoryName(x))
                        .Concat(new[] { Environment.CurrentDirectory })
                        .Distinct()
                        .ToArray(),

                    Parallel = true,
                    CopyAttributes = true,
                    XmlDocumentation = true,
                    UnionMerge = true,

                    DebugInfo = true
                };

                var repacker = new ILRepacking.ILRepack(roptions);
                repacker.Repack();
            }

            patcher = new Engine.Patcher(sourceAsm, new[] { modificationGlob }, outputPath);
            patcher.Run();
        }
    }
}
