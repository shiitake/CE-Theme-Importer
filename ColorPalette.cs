using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.IO;

namespace conemu_theme_import
{
    public class ColorPalette
    {
        private const string FileName = "ConEmu.xml";
        private readonly string _configFile;
        private readonly string _localPath;
        private XDocument Xdoc { get; }
        private XElement Vanilla { get; set; }
        private string Build { get; }
        private int ColorCount { get; set; }

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

        private XmlSchemaSet SchemaSet { get; set; }

        private List<string> InstalledThemes { get; set; }


        private void Log(string message)
        {
            var logDate = DateTime.Now;
            Console.WriteLine(logDate +"\t" + message);
        }

        public ColorPalette(ConfigOptions config)
        {
            var configFolder = config.ConfigurationFolder;
            _configFile = Path.Combine(configFolder, FileName);
            _localPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            SchemaSet = SetXmlSchema(config.XmlValidation);           
            
            Xdoc = XDocument.Load(_configFile);
            Vanilla = Xdoc.Descendants().First(key => (string)key.Attribute("name") == ".Vanilla");
            Build = Vanilla.AttributeOrEmpty("build").Value;

            if (config.BackupConfiguration)
            {
                BackupConfiguration(configFolder, _configFile);
            }

            ColorCount = Vanilla.Descendants().Count(key => (string)key.Attribute("name") == "Colors");

            InstalledThemes = ColorCount == 0 ? new List<string>() : GetInstalledPalettes();
        }

        public void ImportFile(string importFile)
        {
            if (ColorCount == 0)
            {
                InitializeColorPalette();
            }
            Log($"Importing theme from {importFile}.");
            var foundFile = false;
            //make sure file exists
            var myFileInfo = new FileInfo(importFile);
            if (myFileInfo.Exists)
            {
                foundFile = true;
                AddNewPalette(importFile);
                Xdoc.Save(_configFile);
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
                    Xdoc.Save(_configFile);
                }
            }
            if (!foundFile)
            {
                Log($"We were unable to find the file specified: {importFile}");
            }
        }

        public void ImportFolder(string importFolder)
        {
            if (ColorCount == 0)
            {
                InitializeColorPalette();
            }
            var path = (string.IsNullOrWhiteSpace(importFolder)) ? _localPath : importFolder;
            Log($"Looking for any themes in the folder: {path} ");
            var palettes = GetPalettes(path);

            if (palettes.Any())
            {
                Log($"Found {palettes.Count()} files to import");
                foreach (var palette in palettes)
                {
                    AddNewPalette(palette);
                }
                Xdoc.Save(_configFile);
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
                var backupFileName = $"ConEmu_Backup_{backupdate.ToString("yyyyMMdd")}.xml";
                var destination = Path.Combine(configFolder, backupFileName);                
                Log($"Creating backup of configuration file to {destination}");
                File.Copy(configFile, destination);
            }
            catch (Exception e)
            {
                Log($"Failed to backup configuration file with following errors: {e.Message}");
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
            var colors = Vanilla.Descendants().Where(key => (string) key.Attribute("name") == "Colors");

            var name =
                colors.Descendants()
                    .Elements("value")
                    .Where(value => (string) value.Attribute("name") == "Name" && value.AttributeOrEmpty("data").Value != "")
                    .Select(value => value.AttributeOrEmpty("data").Value);

            return name.ToList();
        }

        public void AddNewPalette(string file)
        {
            //validate xml
            if (IsValidXml(file))
            {               
                Vanilla = AddPalette(Vanilla, file);                               
            }
            else
            {
                Log($"Did not import {file} because it did not contain a valid theme.");
            }

        }

        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        private XElement AddPalette(XElement xelem, string file)
        {
            var paletteNumber = ColorCount++;
            var modifiedDate = DateTime.Now;
            var palette = XElement.Load(file);
            var palettename = from key in palette.Descendants()
                   where (string)key.Attribute("name") == "Name"
                   select key.Attribute("data").Value;
            var name = string.Join(",",palettename);
            if (InstalledThemes.Contains(name))
            {
                Log($"The {name} theme is already installed and will not be installed again.");
                return xelem;
            }
            else
            {
                Log($"Adding theme: {name}");
                InstalledThemes.Add(name);
                palette.Attribute("name").SetValue("Palette" + paletteNumber);
                palette.Attribute("modified").SetValue(modifiedDate.ToString("yyyy-MM-dd H:mm:ss"));
                palette.Attribute("build").SetValue(Build);
                var colors = xelem.Descendants().First(key => (string)key.Attribute("name") == "Colors");
                colors.Element("value").Attribute("data").SetValue(paletteNumber.ToString("00000000"));
                colors.Add(palette);
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
                Log($"Validating XML for {xmlFile}");
                while (reader.Read())
                {
                }
                Log($"{xmlFile} is valid XML.");
                return true;
            }
            catch (XmlSchemaException e)
            {
                Log($"The schema for {xmlFile} isn't valid. Line number: {e.LineNumber}, Line position: {e.LinePosition}");
                Log($"Message = {e.Message}");
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
    }
}
