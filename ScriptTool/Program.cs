using CommonLib;
using System;
using System.IO;
using System.Text;

namespace ScriptTool
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("WildBug Script Tool");
                Console.WriteLine("  created by Crsky");
                Console.WriteLine();
                Console.WriteLine("Usage:");
                Console.WriteLine("  Disassemble : ScriptTool -d -in [input.scn] -icp [shift_jis] -out [output.txt]");
                Console.WriteLine("  Export Text : ScriptTool -e -in [input.scn] -icp [shift_jis] -out [output.txt]");
                Console.WriteLine("  Import Text : ScriptTool -i -in [input.scn] -icp [shift_jis] -out [output.scn] -ocp [shift_jis] -txt [input.txt] -p");
                Console.WriteLine("  Import Dir  : ScriptTool -idr -in [input_dir] -icp [shift_jis] -out [output_dir] -ocp [shift_jis] -txt [txt_dir] -p");
                Console.WriteLine();
                Console.WriteLine("Press any key to continue...");

                Environment.ExitCode = 1;
                Console.ReadKey();

                return;
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var parsedArgs = CommandLineParser.ParseArguments(args);

            // Common arguments
            CommandLineParser.EnsureArguments(parsedArgs, "-in", "-icp", "-out");

            var inputPath = Path.GetFullPath(parsedArgs["-in"]);
            var outputPath = Path.GetFullPath(parsedArgs["-out"]);
            var inputEncoding = Encoding.GetEncoding(parsedArgs["-icp"]);

            // Disassemble
            if (parsedArgs.ContainsKey("-d"))
            {
                var script = new Script();
                script.Load(inputPath, inputEncoding);
                script.ExportJson(outputPath);
                return;
            }

            // Export Text
            // Export Text
            if (parsedArgs.ContainsKey("-e"))
            {
                // 文件夹模式
                if (Directory.Exists(inputPath))
                {
                    Directory.CreateDirectory(outputPath);

                    var files = Directory.GetFiles(inputPath, "*.WBI");

                    Console.WriteLine($"[+] Found {files.Length} WBI files");

                    foreach (var file in files)
                    {
                        try
                        {
                            Console.WriteLine();
                            Console.WriteLine($"[+] Processing: {Path.GetFileName(file)}");

                            var script = new Script();

                            script.Load(file, inputEncoding);

                            var outFile = Path.Combine(
                                outputPath,
                                Path.GetFileNameWithoutExtension(file) + ".txt"
                            );

                            script.ExportText(outFile);

                            Console.WriteLine($"[+] Saved: {outFile}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[!] Failed: {file}");
                            Console.WriteLine(ex.Message);
                        }
                    }
                }
                else
                {
                    // 单文件模式
                    var script = new Script();

                    script.Load(inputPath, inputEncoding);

                    script.ExportText(outputPath);
                }

                return;
            }
            // Import Directory
            if (parsedArgs.ContainsKey("-idr"))
            {
                CommandLineParser.EnsureArguments(parsedArgs, "-ocp", "-txt");

                var txtDir = Path.GetFullPath(parsedArgs["-txt"]);
                var outputEncoding = Encoding.GetEncoding(parsedArgs["-ocp"]);

                Directory.CreateDirectory(outputPath);

                var files = Directory.GetFiles(inputPath, "*.WBI");

                Console.WriteLine($"[+] Found {files.Length} WBI files");

                foreach (var file in files)
                {
                    try
                    {
                        var name = Path.GetFileName(file);

                        var txtPath = Path.Combine(
                            txtDir,
                            Path.GetFileNameWithoutExtension(name) + ".txt"
                        );

                        if (!File.Exists(txtPath))
                        {
                            Console.WriteLine($"[SKIP] TXT not found: {name}.txt");
                            continue;
                        }

                        var outFile = Path.Combine(outputPath, name);

                        Console.WriteLine($"[+] Importing: {name}");

                        var script = new Script();

                        script.Load(file, inputEncoding);

                        script.ImportText(txtPath);

                        script.Save(outFile, outputEncoding);

                        // Pack script
                        if (parsedArgs.ContainsKey("-p"))
                        {
                            var data = File.ReadAllBytes(outFile);

                            var wpx = new WpxWriter("EX2");

                            wpx.AddEntry(2, data);

                            wpx.Save(outFile);
                        }

                        Console.WriteLine($"[+] Saved: {outFile}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[!] Failed: {file}");
                        Console.WriteLine(ex.Message);
                    }
                }

                return;
            }
            // Import Text
            if (parsedArgs.ContainsKey("-i"))
            {
                CommandLineParser.EnsureArguments(parsedArgs, "-ocp", "-txt");

                var txtPath = Path.GetFullPath(parsedArgs["-txt"]);
                var outputEncoding = Encoding.GetEncoding(parsedArgs["-ocp"]);

                var script = new Script();
                script.Load(inputPath, inputEncoding);
                script.ImportText(txtPath);
                script.Save(outputPath, outputEncoding);

                // Pack script
                if (parsedArgs.ContainsKey("-p"))
                {
                    var data = File.ReadAllBytes(outputPath);
                    var wpx = new WpxWriter("EX2");
                    wpx.AddEntry(2, data);
                    wpx.Save(outputPath);
                }

                return;
            }
        }
    }
}

