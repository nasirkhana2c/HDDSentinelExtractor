using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDDSentinelExtractor
{
    public class StorageDetails
    {
        public string FileName { get; set; }
        public string Model { get; set; }
        public string DiskSerialNumber { get; set; }
        public string DiskSize { get; set; }
        public string Health { get; set; }
        public string Performance { get; set; }
        public string ArchivePath { get; set; }
        public string FilePath { get; set; }
        
    }
    public class FileStatus
    {
        public string FileName { get; set; }
        public string Message { get; set; }
    }

    public class HardDiskSummary
    {
        public int DiskNumber { get; set; }
        public string SerialNumber { get; set; }
        public string ModelId { get; set; }
        public string TotalSize { get; set; }
        public string Health { get; set; }
        public string Performance { get; set; }

        public override string ToString()
        {
            return $@"
--- Hard Disk Number {DiskNumber} ---
  Serial Number : {SerialNumber}
  Model ID      : {ModelId}
  Total Size    : {TotalSize}
  Health        : {Health}
  Performance   : {Performance}
";
        }
    }


}
