# CE-Theme-Importer
A tool for importing themes into ConEmu. 

CE Theme Importer is a windows command line tool that will allow you to import color themes (aka schemes) into the windows console emulator [http://conemu.github.io/](ConEmu).

It was inspired by the [https://github.com/joonro/ConEmu-Color-Themes](Color Schemes for ConEmu) project. If you're looking for themes to import you should check it out. 

## Using the importer
CE-Theme-Importer does not require installation. Simply run the executable CE-ThemeImporter.exe from the command prompt. If will automatically look for valid files to import in the current directory. And it will ignore any duplicates it finds.

There are also additional options:

Option | Action
--- | ---
-file, -f  <filename> | Specify a single file to import  
-directory,-d <path> | Import files from a specified folder  
-backup, -b |  Backup your existing configuration file
-list, -l | List the custom themes (schemes) that have been installed.
-xml, -x <filename> | Specify a specific xml/xsd file for schema validation. 


