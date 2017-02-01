using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.IO;
using System.Security.Principal;

namespace conemu_theme_import
{
    class Program
    {
        static string debugFolder { get; } = @"C:\git\conemu-theme-import\test";
        static void Main(string[] args)
        {
            PrintLogo();
            var config = GetConfigOptions(args);
            config.ConfigurationFolder = GetConfigFolder();            
            var colorPalette = new ColorPalette(config);

            //if file provided then import just from that file path
            if (!string.IsNullOrWhiteSpace(config.ImportFile))
            {
                colorPalette.ImportFile(config.ImportFile);
            }
            
            //if path provided then import from that path or the local directory            
            if (config.ImportDirectory != "none")
            {
                colorPalette.ImportFolder(config.ImportDirectory);
            }
            
            if (config.ListInstalledPalettes)
            {
                colorPalette.ListInstalledPallettes();
            }
        }

        static string GetConfigFolder()
        {
#if DEBUG
            return debugFolder;
#else
            var winIdent = WindowsIdentity.GetCurrent();
            string name = "";
            if (winIdent != null)
            {
                name = winIdent.Name;
                var index = name.LastIndexOf(@"\", StringComparison.CurrentCulture);
                name = name.Substring(index + 1);            
            }
            return @"C:\Users\" + name + @"\AppData\Roaming\";
#endif
        }

        public static ConfigOptions GetConfigOptions(string[] args)
        {
            var config = new ConfigOptions();
            if (args.Length == 0)
            {
                return config;             
            }
            for (var i = 0; i < args.Length; i++)
            {
                switch(args[i].ToLower())
                {
                    case "-file":
                    case "-f":
                        config.ImportFile = args[i + 1];
                        config.ImportDirectory = "none";
                        break;
                    case "-directory":
                    case "-d":
                        config.ImportDirectory = args[i + 1];
                        break;
                    case "-backup":
                    case "-b":
                        config.BackupConfiguration = true;
                        config.ImportDirectory = "none";
                        break;
                    case "-xml":
                    case "-x":
                        config.XMLValidation = args[i + 1];
                        break;
                    case "-list":
                    case "-l":
                        config.ListInstalledPalettes = true;
                        config.ImportDirectory = "none";
                        break;
                    case "-?":
                    case "-help":
                        PrintHelp();
                        config.ImportDirectory = "none";
                        break;
                }
            }
            return config;
        }

        public static void PrintHelp()
        {
            Console.WriteLine(@"CE-ThemeImporter.exe will attempt to import any color themes (aka schemes) found in the current directory. It will ignore any duplicate themes it finds.");
            Console.WriteLine();
            Console.WriteLine("Usage:" + "\t" + "CE-ThemeImport.exe [-f filename ] [-d directory ] [-b ] [-l]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("\t-file, -f" + "\t\t" + "File containing the theme that you want to import");
            Console.WriteLine("\t-directory, -d" + "\t\t" + "Directory containing multiple themes you want to import. Defaults to current directory");
            Console.WriteLine("\t-backup, -b" + "\t\t" + "Backup your configuration file.");
            Console.WriteLine("\t-list, -l" + "\t\t" + "Lists the currently installed themes.");
            Console.WriteLine("\t-xml, -x" + "\t\t" + "Specify custom xml schema for validation.");
            Console.WriteLine();
            Console.WriteLine("For additional help or if you have problems please refer to our github page: http://www.github.com/shiitake/ce-theme-importer");
            Console.WriteLine("ConEmu is an open source Windows console emulator - http://conemu.github.io.  CE Theme Importer icon is Console Management by Sergey Novosyolov from the Noun Project");
            Console.WriteLine("If you are looking for themes you should check out Color Themes for ConEmu - https://github.com/joonro/ConEmu-Color-Themes ");
        }

        public static void PrintLogo()
        {
            Console.WriteLine("-+-+ +-+-+-+-+-+ +-+-+-+-+-+-+-+-+");
            Console.WriteLine("C|E| |T|h|e|m|e| |I|m|p|o|r|t|e|r|");
            Console.WriteLine("-+-+ +-+-+-+-+-+ +-+-+-+-+-+-+-+-+");
            Console.WriteLine("CE Theme Importer is a simple tool for importing themes into ConEmu. Type -? for help.");
        }

        }
}
