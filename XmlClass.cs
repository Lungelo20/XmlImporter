using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.FtpClient;
using System.Net.Mail;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace XmlImporter
{
    public partial class XmlClass : ServiceBase
    {
        public XmlClass()
        {
            InitializeComponent();
        }
        readonly string connectionString = "Data Source=your_server;Initial Catalog=your_database;User ID=your_username;Password=your_password";

        readonly string smtpServer = "your_smtp_server";
        readonly int smtpPort = 587;
        readonly string smtpUsername = "your_smtp_username";
        readonly string smtpPassword = "your_smtp_password";
        readonly string senderEmail = "your_sender_email";
        readonly string recipientEmail = "your_recipient_email";

        readonly string ftpServer = "ftp://your_ftp_server";
        readonly string ftpUsername = "your_ftp_username";
        readonly string ftpPassword = "your_ftp_password";
        readonly string ftpFolderPath = "/path/to/xml/files";

        protected override void OnStart(string[] args)
        {
            try
            {
                var xmlData = new XmlData();
                // This is where we locate and connect to ftp
                using (var ftpClient = new FtpClient())
                {
                    ftpClient.Credentials.Domain = ftpServer;
                    ftpClient.Credentials.UserName = ftpUsername;
                    ftpClient.Credentials.Password = ftpPassword;
                    ftpClient.Connect();

                    var ftpFiles = ftpClient.GetListing(ftpFolderPath)
                        .Where(x => x.Type == FtpFileSystemObjectType.File && x.Name.EndsWith(".xml"))
                        .ToList();

                    foreach (var ftpFile in ftpFiles)
                    {
                        using (var ftpStream = ftpClient.OpenRead(ftpFile.FullName))
                        {
                            // Read XML file and perform necessary operations
                            XDocument xmlDoc = XDocument.Load(ftpStream);
                            XElement rootElement = xmlDoc.Root;

                            // Iterate over the child elements of the root element using a foreach loop
                            foreach (XElement childElement in rootElement.Elements())
                            {
                                // Access and process each child element as needed
                                 xmlData.Value2 = childElement.Value;

                                // Perform your logic here...
                            }

                        }
                    }
                    //Call the udate function
                    UpdateDatabaseRecord(xmlData);

                    ftpClient.Disconnect();
                }
                          
                // Start your XML processing logic here

                SendEmail("Service Started", "The service has started successfully.");
            }
            catch (Exception ex)
            {
                SendEmail("Service Start Error", "An error occurred while starting the service:\n\n" + ex.Message);
                throw; // Rethrow the exception to indicate a service start failure
            }

        }

        private void UpdateDatabaseRecord(XmlData xmlData)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                // Check if record exists in the database
                string selectQuery = "SELECT COUNT(*) FROM TableName WHERE Id = @Id";
                SqlCommand selectCommand = new SqlCommand(selectQuery, connection);
                selectCommand.Parameters.AddWithValue("@Id", xmlData.Id);

                int recordCount = (int)selectCommand.ExecuteScalar();

                if (recordCount > 0)
                {
                    // Update the existing record
                    string updateQuery = "UPDATE TableName SET Column1 = @Value1, Column2 = @Value2 WHERE Id = @Id";
                    SqlCommand updateCommand = new SqlCommand(updateQuery, connection);
                    updateCommand.Parameters.AddWithValue("@Id", xmlData.Id);
                    updateCommand.Parameters.AddWithValue("@Value1", xmlData.Value1);
                    updateCommand.Parameters.AddWithValue("@Value2", xmlData.Value2);
                    // Add more parameters as needed

                    updateCommand.ExecuteNonQuery();
                }
                else
                {
                    //NEEDS MORE WORK
                    // Insert a new record
                    string insertQuery = "INSERT INTO TableName (Id, Column1, Column2, ...) VALUES (@Id, @Value1, @Value2, ...)";
                    SqlCommand insertCommand = new SqlCommand(insertQuery, connection);
                    insertCommand.Parameters.AddWithValue("@Id", xmlData.Id);
                    insertCommand.Parameters.AddWithValue("@Value1", xmlData.Value1);
                    insertCommand.Parameters.AddWithValue("@Value2", xmlData.Value2);
                    // Add more parameters as needed

                    insertCommand.ExecuteNonQuery();
                }
                if (!string.IsNullOrEmpty(""))
                {
                    //NEEDS MORE WORK
                    // Delete the record from the database
                    string deleteQuery = "DELETE FROM TableName WHERE RecordId = @RecordId";
                    SqlCommand deleteCommand = new SqlCommand(deleteQuery, connection);
                    deleteCommand.Parameters.AddWithValue("@RecordId", "");
                    deleteCommand.ExecuteNonQuery();
                }
            }
        }
    
        private void SendEmail(string subject, string body)
        {
            using (var smtpClient = new SmtpClient(smtpServer, smtpPort))
            {
                smtpClient.UseDefaultCredentials = false;
                smtpClient.EnableSsl = true;
                smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

                using (var mailMessage = new MailMessage(senderEmail, recipientEmail, subject, body))
                {
                    smtpClient.Send(mailMessage);
                }
            }
        }


        protected override void OnStop()
        {
            try
            {
                // Stop your XML processing logic here

                SendEmail("Service Stopped", "The service has stopped.");
            }
            catch (Exception ex)
            {
                SendEmail("Service Stop Error", "An error occurred while stopping the service:\n\n" + ex.Message);
            }
        }
    }
}
