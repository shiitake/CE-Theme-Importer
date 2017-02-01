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
    public class ColorPalette
    {
        private const string _fileName = "ConEmu.xml";
        private string _configFolder;        
        private string _configFile;
        private string _localPath;
        XDocument Xdoc { get; set; }
        XElement Vanilla { get; set; }
        string Build { get; set; }
        int ColorCount { get; set; }

        private const string DefaultSchema = @"<?xml version='1.0' encoding='IBM437'?>
<xs:schema attributeFormDefault='unqualified' elementFormDefault='qualified' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
  <xs:element name='key'>
    <xs:complexType>
      <xs:sequence>
        <xs:element maxOccurs='unbounded' name='value'>
          <xs:complexType>
            <xs:attribute name='name' type='xs:string' use='required' />
            <xs:attribute name='type' type='xs:string' use='required' />
            <xs:attribute name='data' type='xs:string' use='required' />
          </xs:complexType>
        </xs:element>
      </xs:sequence>
      <xs:attribute name='name' type='xs:string' use='required' />
      <xs:attribute name='modified' type='xs:string' use='required' />
      <xs:attribute name='build' type='xs:unsignedInt' use='required' />
    </xs:complexType>
  </xs:element>
</xs:schema>";

        XmlSchemaSet SchemaSet { get; set; }

        List<string> InstalledThemes { get; set; }


        private void Log(string message)
        {
            var logDate = DateTime.Now;
            Console.WriteLine(logDate +"\t" + message);
        }

        public ColorPalette(ConfigOptions config)
        {
            _configFolder = config.ConfigurationFolder;
            _configFile = Path.Combine(_configFolder, _fileName);
            _localPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            SchemaSet = SetXmlSchema(config.XMLValidation);           

            Xdoc = XDocument.Load(_configFile, LoadOptions.PreserveWhitespace);
            Vanilla = Xdoc.Descendants().First(key => (string)key.Attribute("name") == ".Vanilla");
            Build = Vanilla.AttributeOrEmpty("build").Value.ToString();

            if (config.BackupConfiguration)
            {
                BackupConfiguration(_configFolder, _configFile);
            }

            ColorCount = Vanilla.Descendants().Count(key => (string)key.Attribute("name") == "Colors");

            if (ColorCount == 0)
            {                
                InstalledThemes = new List<string>();
            }
            else
            {
                InstalledThemes = GetInstalledPalettes();
            }
        }

        public void ImportFile(string importFile)
        {
            if (ColorCount == 0)
            {
                InitializeColorPalette();
            }
            Log(string.Format("Importing theme from {0}.", importFile));
            var foundFile = false;
            //make sure file exists
            var myFileInfo = new FileInfo(importFile);
            if (myFileInfo.Exists)
            {
                foundFile = true;
                AddNewPalette(importFile);
            }
            else
            {
                //lets see if we can find the file name in the local directory
                var localFile = Path.Combine(_localPath, importFile);
                var myLocalFileInfo = new FileInfo(localFile);
                if (myLocalFileInfo.Exists)
                {
                    foundFile = true;
                    AddNewPalette(localFile);
                }
            }
            if (!foundFile)
            {
                Log(string.Format("We were unable to find the file specified: {0}", importFile));
            }
        }

        public void ImportFolder(string importFolder)
        {
            if (ColorCount == 0)
            {
                InitializeColorPalette();
            }
            var path = (string.IsNullOrWhiteSpace(importFolder)) ? _localPath : importFolder;
            Log(string.Format("Looking for any themes in the folder: {0} ", path));
            var palettes = GetPalettes(path);

            if (palettes.Any())
            {
                Log(string.Format("Found {0} files to import", palettes.Count()));
                foreach (var palette in palettes)
                {
                    AddNewPalette(palette);
                }
            }
            else
            {
                Log("No valid files found.");
            }
        }

        private string[] GetPalettes(string location)
        {
            var allXmlFiles = Directory.GetFiles(location, "*.xml", SearchOption.TopDirectoryOnly);
            return allXmlFiles;
        }

        public void ListInstalledPallettes()
        {
            var installList = GetInstalledPalettes();
            if (installList.Count > 0)
            {
                Log("We found the following custom themes installed:");
                foreach(var install in installList)
                {
                    Log("\t" + install);
                }
            }
            else
            {
                Log("We didn't find any custom themes installed.");
            }
            
        }

        private void BackupConfiguration(string configFolder, string configFile)
        {
            try
            {
                var backupdate = DateTime.Now;
                var backupFileName = string.Format("ConEmu_Backup_{0}.xml", backupdate.ToString("yyyyMMdd"));
                var destination = Path.Combine(configFolder, backupFileName);                
                Log(string.Format("Creating backup of configuration file to {0}", destination));
                File.Copy(configFile, destination);
            }
            catch (Exception e)
            {
                Log(string.Format("Failed to backup configuration file with following errors: {0}", e.Message));
            }
        }

        private void InitializeColorPalette()
        {
            Log("Initializing Color Palettes");
            var date = DateTime.Now;
            var defaultPallette =
                new XElement("key",
                    new XAttribute("name", "Colors"),
                    new XAttribute("modified", date.ToString("yyyy-MM-dd H:mm:ss")),
                    new XAttribute("build", Build),
                    new XElement("value",
                        new XAttribute("name", "Count"),
                        new XAttribute("type", "dword"),
                        new XAttribute("data", ColorCount.ToString("00000000"))
                        )
                );
            Vanilla.Add(defaultPallette);
            ColorCount++;
        }

        private List<string> GetInstalledPalettes()
        {
            var colors = from key in Vanilla.Descendants()
                         where (string)key.Attribute("name") == "Colors"
                         select key;

            var name = from value in colors.Descendants().Elements("value")
                       where (string)value.Attribute("name") == "Name"
                       select value.Attribute("data").Value.ToString();

            return name.ToList();
        }

        public void AddNewPalette(string file)
        {
            //validate xml
            if (IsValidXml(file))
            {                
                string paletteName;
                Vanilla = AddPalette(Vanilla, file);                                
                Xdoc.Save(_configFile);
            }
            else
            {
                Log(string.Format("Did not import {0} because it did not contain a valid theme.", file));
            }

        }

        private XElement AddPalette(XElement xelem, string file)
        {
            var paletteNumber = ColorCount++;
            var modifiedDate = DateTime.Now;
            var palette = XElement.Load(file);
            var palettename = from key in palette.Descendants()
                   where (string)key.Attribute("name") == "Name"
                   select key.Attribute("data").Value.ToString();
            var name = string.Join(",",palettename);
            if (InstalledThemes.Contains(name))
            {
                Log(string.Format("The {0} theme is already installed and will not be installed again.", name));
                return xelem;
            }
            else
            {
                Log(string.Format("Adding theme: {0}", name));
                InstalledThemes.Add(name);
                palette.Attribute("name").SetValue("Palette" + paletteNumber.ToString());
                palette.Attribute("modified").SetValue(modifiedDate.ToString("yyyy-MM-dd H:mm:ss"));
                palette.Attribute("build").SetValue(Build);
                var Colors = xelem.Descendants().First(key => (string)key.Attribute("name") == "Colors");
                Colors.Element("value").Attribute("data").SetValue(paletteNumber.ToString("00000000"));
                Colors.Add(palette);
                return xelem;
            }
        }

        private bool IsValidXml(string xmlFile)
        {
            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ValidationType = ValidationType.Schema;
            settings.Schemas = SchemaSet; 

            XmlReader reader = XmlReader.Create(xmlFile, settings);
            try
            {
                Log(string.Format("Validating XML for {0}", xmlFile));
                while (reader.Read()) ;
                Log(string.Format("{0} is valid XML.", xmlFile));
                return true;
            }
            catch (XmlSchemaException e)
            {
                Log(string.Format("The schema for {0} isn't valid. Line number: {1}, Line position: {2}", xmlFile, e.LineNumber, e.LinePosition));
                Log(string.Format("Message = {0}", e.Message));
                return false;
            }

        }

        private XmlSchemaSet SetXmlSchema(string schemaFile)
        {
            SchemaSet = new XmlSchemaSet();
            if (!string.IsNullOrWhiteSpace(schemaFile))
            {                
                SchemaSet.Add(null, schemaFile);
            }
            else
            {
                using (XmlReader xmlReader = XmlReader.Create(new StringReader(DefaultSchema)))
                {
                    SchemaSet.Add(null, xmlReader);
                }
            }
            return SchemaSet;
        }

        private void GenerateSchema(string xmlFile)
        {
            var reader = XmlReader.Create(xmlFile);
            var schemaSet = new XmlSchemaSet();
            var schema = new XmlSchemaInference();
            schemaSet = schema.InferSchema(reader);
            FileStream file = new FileStream("config.xsd", FileMode.Create, FileAccess.ReadWrite);
            //using (XmlTextWriter xwriter = new XmlTextWriter(file, new UTF8Encoding()))
            using (StreamWriter sw = new StreamWriter(file))
            {
                //Console.SetOut(sw);

                foreach (XmlSchema s in schemaSet.Schemas())
                {

                    //s.Write(sw);
                    s.Write(Console.Out);
                }
            }
        }
    }
}
