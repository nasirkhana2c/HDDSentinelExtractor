using HDDSentinelExtractor;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;


namespace HDDSentinelExtractor
{
    class Program
    {
        //static void Main(string[] args)
        static void Main(string[] args)
        {
            dbLayer db = new dbLayer();
            List<StorageDetails> lstStorageDetails = new List<StorageDetails>();
            List<FileStatus> lstFileStatus = new List<FileStatus>();
            string WH1Path = ConfigurationManager.AppSettings["WH1Path"];
            string WH2Path = ConfigurationManager.AppSettings["WH2Path"];
            string WH1Archive = ConfigurationManager.AppSettings["WH1ArchivePath"];
            string WH2Archive = ConfigurationManager.AppSettings["WH2ArchivePath"];

            lstStorageDetails = GetTextFileNamesFromFolder(WH1Path, WH1Archive);
            lstStorageDetails.AddRange(GetTextFileNamesFromFolder(WH2Path, WH2Archive));
            if (lstStorageDetails.Count() > 0)
            {
                lstFileStatus = db.SaveFileStorageDetails(lstStorageDetails);
            }
            if (lstFileStatus.Count() > 1)
            {
                ProcessFiles(lstFileStatus, lstStorageDetails);
            }
        }

        public static bool MoveFiles(string archivePath, string filePath)
        {
            string destFile = Path.Combine(archivePath, Path.GetFileName(filePath));

            if (File.Exists(filePath))
            {
                if (File.Exists(destFile))
                {
                    File.Delete(destFile);
                }
                File.Move(filePath, destFile);
            }


            return true;
        }
        public static void ProcessFiles(List<FileStatus> lstFileStatus, List<StorageDetails> lstStorageDetails)
        {
            string archivePath;
            string WH1FailedPath = ConfigurationManager.AppSettings["WH1FailedPath"];
            string WH2FailedPath = ConfigurationManager.AppSettings["WH2FailedPath"];

            // Loop through the files and check their status
            foreach (var file in lstStorageDetails)
            {
                string currentFilePath = file.FilePath;

                // Check if the file has a recorded status
                var fileStatus = lstFileStatus.Where(x => x.FileName == file.FileName).FirstOrDefault();
                
                if (fileStatus.Message == "Successfull")
                {
                    MoveFiles(file.ArchivePath, currentFilePath);
                }
                else
                {
                    if (file.ArchivePath.Contains("WH1"))
                    {
                        archivePath = WH1FailedPath;
                    }
                    else
                    {
                        archivePath = WH2FailedPath;
                    }
                    MoveFiles(archivePath, currentFilePath);
                }
            }
        }
        public static List<StorageDetails> GetTextFileNamesFromFolder(string folderPath, string archivePath)
        {
            List<StorageDetails> lst = new List<StorageDetails>();
            //string folderPath = @"C:\Users\user\Downloads\HDSenti";

            if (!Directory.Exists(folderPath))
            {
                Console.WriteLine($"Folder not found: {folderPath}");
                return lst;
            }

            string[] files = Directory.GetFiles(folderPath, "*.txt");

            if (files.Length == 0)
            {
                Console.WriteLine("No .txt files found in the folder.");
                return lst;
            }


            foreach (string file in files)
            {
                try
                {
                    lst.AddRange(DiskReportParser.ParseReport(file, archivePath));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  Error parsing file: {ex.Message}\n");
                }
            }
            if (lst == null)
            {
                lst = new List<StorageDetails>();
            }
            return lst;
        }
        public static List<StorageDetails> GetFileNamesFromFolder()
        {
            string fileName = "";
            List<StorageDetails> lst = new List<StorageDetails>();
            StorageDetails obj;
            dbLayer db = new dbLayer();
            List<string> lstDBFileNames = db.GetExistingFilesNames();

            string folderPath = @"C:\AppData\StorageFile";
            string archivePath = @"C:\AppData\StorageFile\Archieve";
            //string folderPath = @"C:\Users\user\Downloads\HDSenti";
            //string archivePath = @"C:\Users\user\Downloads\HDSenti\Archieve";

            var files = Directory.GetFiles(folderPath, "*.xml").ToList<string>();

            //files = files.Where(f => !lstDBFileNames.Contains(f)).ToList();
            foreach (var file in files)
            {
                fileName = Path.GetFileName(file);
                if (lstDBFileNames.IndexOf(fileName) >= 0)
                {
                    continue;
                }
                try
                {
                    XDocument doc = XDocument.Load(file);

                    //var disks = doc.Root
                    //               .Elements()
                    //               .Where(x => x.Name.LocalName.StartsWith("Physical_Disk_Information_Disk_"));

                    var disks = doc.XPathSelectElements("//*[starts-with(name(),'Physical_Disk_Information_Disk_')]");

                    foreach (var disk in disks)
                    {
                        var summary = disk.Element("Hard_Disk_Summary");

                        if (summary != null)
                        {
                            obj = new StorageDetails();
                            obj.FileName = fileName;
                            obj.Model = summary.Element("Hard_Disk_Model_ID")?.Value;
                            obj.DiskSerialNumber = summary.Element("Hard_Disk_Serial_Number")?.Value;
                            obj.DiskSize = summary.Element("Total_Size")?.Value;
                            obj.Health = summary.Element("Health")?.Value;
                            obj.Performance = summary.Element("Performance")?.Value;
                            lst.Add(obj);
                        }
                    }

                    string destFile = Path.Combine(archivePath, Path.GetFileName(file));

                    // If file already exists, overwrite
                    if (File.Exists(destFile))
                        File.Delete(destFile);

                    File.Move(file, destFile);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error reading file: " + ex.Message);
                }
            }





            return lst;
        }
    }

    public class DiskReportParser
    {
        public static List<StorageDetails> ParseReport(string filePath, string archivePath)
        {
            //string archivePath = @"C:\Users\user\Downloads\HDSenti\Archieve";

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Report file not found: {filePath}");

            string content = File.ReadAllText(filePath);
            var lst = new List<StorageDetails>();

            var disks = new StorageDetails();

            var diskSections = Regex.Split(content, @"(?=--\s*Physical Disk Information\s*-\s*Disk:\s*#\d+)");

            foreach (var section in diskSections)
            {
                if (!section.Contains("Hard Disk Summary"))
                    continue;
                disks = new StorageDetails();
                disks.FileName = Path.GetFileName(filePath);
                disks.DiskSerialNumber = ExtractValue(section, @"Hard Disk Serial Number\s*[\.\s]+:\s*(.+)");
                disks.Model = ExtractValue(section, @"Hard Disk Model ID\s*[\.\s]+:\s*(.+)");
                disks.DiskSize = ExtractValue(section, @"Total Size\s*[\.\s]+:\s*(.+)");
                disks.Health = ExtractCleanValue(ExtractValue(section, @"Health\s*[\.\s]+:\s*(.+)"));
                disks.Performance = ExtractCleanValue(ExtractValue(section, @"Performance\s*[\.\s]+:\s*(.+)"));
                disks.ArchivePath = archivePath;
                disks.FilePath = filePath;
                lst.Add(disks);
                // ✅ NO File.Move here — file is still being read
            }
            if (!Directory.Exists(archivePath))
            {
                Console.WriteLine($"Archive folder not found, skipping move: {archivePath}");
            }

            // ✅ Move AFTER the loop, once reading is fully done
            /*
            if (Directory.Exists(archivePath))
            {
                string destFile = Path.Combine(archivePath, Path.GetFileName(filePath));

                if (File.Exists(destFile))
                    File.Delete(destFile);

                File.Move(filePath, destFile);
            }
            else
            {
                Console.WriteLine($"Archive folder not found, skipping move: {archivePath}");
            }
            */
            return lst;
        }

        private static string ExtractValue(string text, string pattern)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.Trim() : "N/A";
        }
        private static string ExtractCleanValue(string raw)
        {
            // Strip leading # and - characters, trim whitespace
            return Regex.Replace(raw, @"^[#\-\s]+", "").Trim();
        }

        private static int ExtractInt(string text, string pattern)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value.Trim(), out int result))
                return result;
            return -1;
        }
    }
}
