using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace conemu_theme_import
{
    public class ConfigOptions
    {
        public string ImportFile { get; set; } = "";
        public string ImportDirectory { get; set; } = "";
        public string ConfigurationFolder { get; set; }
        public bool BackupConfiguration { get; set; } = false;

        public bool ListInstalledPalettes { get; set; } = false;
        public string XMLValidation { get; set; } = "";
    }
}
