using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ax7LabelsReplacer
{
    class Program
    {
        static string modelFolderPath = @"J:\AosService\PackagesLocalDirectory\ApplicationSuite\MyModelFolder\";
        static string connectionString = @"Data Source=DEVSQL;Integrated Security=True;Connect Timeout=15;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";

        static void Main(string[] args)
        {
            Program replacer = new Program();

            foreach (string label in LabelsList.get())//.Take(2))
            {
                string newLabel = $"@NewLabelsFile:NEW{label.Replace(@"@OLD", "")}";
                replacer.replaceLabelInSrcXml(label, newLabel);
            }
        }

        public void replaceLabelInSrcXml(string _fromLabel, string _toLabel)
        {
            List<string> srcXmlFilesList = findLabelReference(_fromLabel);
            
            if (srcXmlFilesList.Count == 0)
            {
                Console.WriteLine($"Label {_fromLabel} abandoned");
            }
            else
            {
                foreach (string srcXmlFilePath in srcXmlFilesList)
                {
                    string text = File.ReadAllText(srcXmlFilePath);
                    text = text.Replace(_fromLabel, _toLabel);
                    File.WriteAllText(srcXmlFilePath, text);

                    if (!text.Contains(_fromLabel))
                    {
                        Console.WriteLine($"Warning: Label {_fromLabel} is not found in {srcXmlFilePath}. Cross references out of date");
                    }
                    else
                    { 
                        Console.WriteLine($"Label {_fromLabel} replaced with {_toLabel} in {srcXmlFilePath}");
                    }
                }
            }
        }

        private List<string> findLabelReference(string _searchingObjectName)
        {
            List<string> retList = new List<string>();
            //string sqlRefRequest = $@"use DYNAMICSXREFDB; select * from XRefsTmpTable where searchObjectPath like '%{_searchingObjectName}%'";
            //To speed up the SQL queuing, insert selected result to separated table
            string sqlRefRequest = $@"
                use DYNAMICSXREFDB;
                select
	                modules.Module
	                ,searchObject.Id as 'SearchObjectId'
	                ,searchObject.Path as 'SearchObjectPath'
	                ,xrefs.SourceId as 'ReferencedObjectId'
	                ,referencedObject.[Path] as 'ReferencedObjectPath'
	                ,xrefs.Line
	                ,xrefs.[Column]
	                ,xrefs.Kind
                from [Names] searchObject
                inner join [Modules] modules
	                on modules.Id = searchObject.ModuleId
                inner join [References] xrefs
	                on xrefs.TargetId = searchObject.Id
                inner join [Names] referencedObject
	                on referencedObject.Id = xrefs.SourceId
                where searchObject.Path like '%{_searchingObjectName}%'";
            try
            {
                SqlConnection sqlConnection = new SqlConnection(connectionString);

                sqlConnection.Open();

                using (SqlCommand command = new SqlCommand(sqlRefRequest, sqlConnection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (! reader.HasRows)
                        {
                            return retList;
                        }

                        while (reader.Read())
                        {
                            string objectPath = reader.GetString(4);
                            try
                            {
                                retList.Add(findSrcXmlFile(objectPath));
                            }
                            catch (Exception _ex)
                            {
                                Console.WriteLine($"XML error: {_ex.Message}");
                            }
                        }
                    }
                }
            }
            catch (Exception _ex)
            {
                Console.WriteLine($"SQL error: {_ex.Message}");
            }

            return retList;
        }

        private string findSrcXmlFile(string _referencedObjectPath)
        { 
            string type;
            string fileName;

            string[] strParts = _referencedObjectPath.Split('/');

            if (_referencedObjectPath[0] == '/')
            {
                type = strParts[1];
                fileName = strParts[2];
            }
            else
            {
                type = strParts[0];
                fileName = strParts[1];
            }

            type = type.Replace("Classes", "Class");
            type = type.Replace("Tables", "Table");
            type = type.Replace("Forms", "Form");

            fileName = fileName.Split('?')[0];
            fileName = $"{fileName}.xml";

            string[] filesList = Directory.GetFiles(modelFolderPath, fileName, SearchOption.AllDirectories);

            if (filesList.Length > 1)
            {
                string[] dirsList = Directory.GetDirectories(modelFolderPath, $"Ax{type}", SearchOption.AllDirectories);

                if (dirsList.Length > 1)
                {
                    throw new Exception($"Directories count > 1 {fileName}");
                }

                if (dirsList.Length == 0)
                { 
                    throw new Exception($"Directories count = 0 {fileName}");
                }

                filesList = Directory.GetFiles(dirsList[0], fileName, SearchOption.AllDirectories);

                if (filesList.Length > 1)
                {
                    throw new Exception($"Sources count > 1 {fileName}");
                }
            }

            if (filesList.Length == 0)
            {
                throw new Exception($"Sources count = 0 {fileName}");
            }

            return filesList[0];
        }
    }
}
