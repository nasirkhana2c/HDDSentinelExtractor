using HDDSentinelExtractor;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace HDDSentinelExtractor
{
    public class dbLayer
    {
        string connectionString = ConfigurationManager.ConnectionStrings["A2CConnectionString"].ToString();

        public List<string> GetExistingFilesNames()
        {
            List<string> lstFileNames = new List<string>();

            using (SqlConnection myConnection = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("spGetExistingHDDSentinelFilesNames", myConnection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    myConnection.Open();

                    //SqlParameter Inhouse = cmd.Parameters.AddWithValue("@InhouseSerialNo", newListForPass);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            lstFileNames.Add(dr["FileName"].ToString());
                        }
                    }
                }
            }
            return lstFileNames;
        }

        public List<FileStatus> SaveFileStorageDetails(List<StorageDetails> lstStorage)
        {
            List<FileStatus> lstFileStatus = new List<FileStatus>();
            FileStatus obj;
            var dataTable = new DataTable();
            dataTable.Columns.Add("FileName", typeof(string));
            dataTable.Columns.Add("Model", typeof(string));
            dataTable.Columns.Add("DiskSerialNumber", typeof(string));
            dataTable.Columns.Add("DiskSize", typeof(string));
            dataTable.Columns.Add("Health", typeof(string));
            dataTable.Columns.Add("Performance", typeof(string));

            foreach (var item in lstStorage)
            {
                dataTable.Rows.Add(item.FileName, item.Model, item.DiskSerialNumber, item.DiskSize, item.Health, item.Performance);
            }

            using (SqlConnection myConnection = new SqlConnection(connectionString))
            {

                using (SqlCommand cmd = new SqlCommand("spSaveHDDSentinelFileDetails", myConnection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    myConnection.Open();

                    cmd.Parameters.AddWithValue("@HDDSentinelDetail", dataTable);

                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            obj = new FileStatus();
                            obj.FileName = dr["FileName"].ToString();
                            obj.Message = dr["Message"].ToString();
                            lstFileStatus.Add(obj);
                        }
                    }
                }
            }

            return lstFileStatus;
        }
    }
}
