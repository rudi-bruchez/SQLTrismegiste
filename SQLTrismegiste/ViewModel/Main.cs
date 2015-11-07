using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.IO.Compression;
using System.IO;
using System.Net.Mail;
using SQLTrismegiste.Tools;
using System.Diagnostics;
using System.Configuration;
using System.Data;
using System.Deployment.Application;
using SimpleLogger;
using SQLTrismegiste.CorpusManager;
using System.Xml;
using System.Windows.Input;

/* TODO
* internationalisation
* ajouter du corpus
* faire la table des matières HTML
* ajouter des commentaires
* préparer une documentation minimale anglais / français
* check all
* finir le cache extractor
*/

namespace SQLTrismegiste.ViewModel
{
    /// <summary>
    /// Main ViewModel for the SQLTrismegiste GUI
    /// </summary>
    internal sealed class Main : INotifyPropertyChanged
    {
        #region private members
        // ----- private readonly members -----
        private static readonly string _outputRoot =
            $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\\SQLTrismegiste\\";
        private readonly BackgroundWorker _worker = new BackgroundWorker();
        private readonly Configuration _config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        private CorpusManager.Manager _ce;

        private SqlServer.ServerInfo _infos = new SqlServer.ServerInfo();

        #endregion

        #region public members for binding

        /// <summary>
        /// the SqlConnectionStringBuilder used for all database access and WPF binding. It is stored in the app config
        /// file on closing and loaded on starting.
        /// </summary>
        /// binding properties need to be public to work in WPF ! (but the class itself need not be...)
        public SqlConnectionStringBuilder ConnectionStringBuilder { get; set; } = new SqlConnectionStringBuilder()
        {
            IntegratedSecurity = true,
            Pooling = true,
            ApplicationName = $"SQL Trismegiste {Version}",
            InitialCatalog = "master"
        };

        // --------------------------------------------------------------------------------
        // SqlConnectionStringBuilder.UserID and Password are ReadOnly ? In certain cases ?
        // anyway it breaks the TwoBinding in wpf, so I need to set it indirectly (?)
        public string UserID
        {
            get { return ConnectionStringBuilder.UserID; }
            set { ConnectionStringBuilder.UserID = value; }
        }
        public string Password
        {
            get { return ConnectionStringBuilder.Password; }
            set { ConnectionStringBuilder.Password = value; }
        }
        // --------------------------------------------------------------------------------

        // list of databases to check - bound to datagrid in GUI
        public ObservableCollection<SqlServer.Database> Databases { get; set; } = new ObservableCollection<SqlServer.Database>();

        // binding properties
        public string OutputPath { get; private set; }
        public string Email { get; set; }
        public string ServerInfos { get; private set; }

        internal void SaveZip()
        {
            var filename = ZipResults();
            var dest = $"{_outputRoot}{Path.GetFileName(filename)}";
            File.Move(filename, dest);
            MessageBox.Show($"file saved to {dest}", "OK", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public string StatusText { get; private set; }
        public string ApplicationTitle { get; private set; }
        public ProcessingOptions Options { get; set; } = new ProcessingOptions();

        public static string Version
        {
            get
            {
                try {
                    var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                    return assembly.GetName().Version.ToString();
                }
                catch (InvalidDeploymentException) { return "[debug]"; }
            }
        }

        public IEnumerable<string> SqlServers
        {
            get
            {
                var servers =
                        from s in System.Data.Sql.SqlDataSourceEnumerator.Instance.GetDataSources().AsEnumerable()
                        select s.Field<string>("ServerName"); // InstanceName
                return servers.ToList();
            }
        }

        // Treeview
        private List<Folder> _folders;
        public List<Folder> Folders
        {
            get { return _folders; }
            set
            {
                _folders = value;
                OnPropertyChanged("Folders"); // why doesn't it call here when we set it in the background worker ... because no set ?
                //OnPropertyChanged("Items");
            }
        }

        #endregion

        #region implementing INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            //if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName)); // null propagation !!
            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region constructor and background worker
        public Main()
        {
            System.IO.Directory.CreateDirectory(_outputRoot);
            //OutputPath = 
            //(new System.IO.FileInfo(OutputPath)).Directory.Create(); // If the directory already exists, this method does nothing.
            _worker.DoWork += worker_RunAnalysis;
            _worker.RunWorkerCompleted += worker_RunAnalysisCompleted;
            _worker.ProgressChanged += worker_RunAnalysisProgressChanged;
            _worker.WorkerReportsProgress = true;
            try
            {
                LoadConfig();
            }
            catch (Exception)
            {
                // silence;                
            }
            StatusText = "Not connected";  //{DynamicResource NotConnected}
            OnPropertyChanged("StatusText"); // => make the class sealed. Calling private method in constructor

            ApplicationTitle = $"SQL Trismegiste {Version}"; // TODO DynamicResource Title

            _ce = new CorpusManager.Manager(
                new System.Data.SqlClient.SqlConnection(ConnectionStringBuilder.ConnectionString),
                Databases.Where(d => d.Checked).Select(d => d.Name).ToList(),
                _infos,
                Options
                );
        }

        private void worker_RunAnalysis(object sender, DoWorkEventArgs e)
        {
            var i = 1;
            var nb = _ce.Hermetica.Count;
            foreach (var hermeticus in _ce.Hermetica)
            {
                _ce.Run(hermeticus);
                _worker.ReportProgress(i, hermeticus);
                i++;
            }
        }

        /// <summary>
        /// TODO
        /// </summary>
        internal void ValidateHermeticus()
        {
            //throw new NotImplementedException();
        }

        private void worker_RunAnalysisCompleted(object sender,
                                               RunWorkerCompletedEventArgs e)
        {
            //update ui once worker complete his work
            var msg = $"Analysis of {_infos.ServerName} completed";
            MessageBox.Show(msg, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            // status
            StatusText = msg;
            OnPropertyChanged("StatusText");
            Mouse.OverrideCursor = null;
        }

        private void worker_RunAnalysisProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var item = (e.UserState as Hermeticus);
            if (item != null)
            {
                Debug.Print(item.Name);

                // status
                StatusText = $"Hermetica [{item.Name}] analysed. Analysis still running. You can get already see the first results...";
                OnPropertyChanged("StatusText");
                OnPropertyChanged("Folders.Hermetica");
            }
        }
        #endregion

        #region instance methods

        public void PopulateFolders()
        {
            _folders = new List<Folder>();

            var doc = new XmlDocument();

            try
            {
                doc.Load("CorpusHermeticum/Folders.xml");
                var xmlNodeList = doc.DocumentElement?.SelectNodes("/folders/folder");
                if (xmlNodeList == null) return;
                foreach (XmlNode folder in xmlNodeList)
                {
                    if (folder.Attributes == null) continue;
                    var selectSingleNode = folder.SelectSingleNode("display[@lang='FR']");
                    if (selectSingleNode?.Attributes == null) continue; // if (selectSingleNode == null) continue; if (selectSingleNode.Attributes == null) continue;

                    var tf = new Folder
                    {
                        Display = selectSingleNode.Attributes["label"].Value,
                        Tooltip = selectSingleNode.Attributes["tooltip"].Value,
                        Name = folder.Attributes["name"].Value,
                    };
                    tf.Hermetica.AddRange(_ce.Hermetica.Where(h => h.FolderName == tf.Name));
                    Folders.Add(tf);
                }

            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "error loading folders", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void LoadConfig()
        {
            if (_config.AppSettings.Settings["Email"] != null)
            {
                Email = _config.AppSettings.Settings["Email"].Value;
                OnPropertyChanged("Email");
            }

            if (_config.ConnectionStrings.ConnectionStrings["last"] == null) return;

            ConnectionStringBuilder.ConnectionString = _config.ConnectionStrings.ConnectionStrings["last"].ConnectionString;

            OnPropertyChanged("ConnectionStringBuilder");
            OnPropertyChanged("UserName");
            OnPropertyChanged("Password");
        }

        internal void SaveConfig()
        {
            if (_config.AppSettings.Settings["Email"] != null)
            {
                _config.AppSettings.Settings["Email"].Value = Email;
            }
            else
            {
                _config.AppSettings.Settings.Add("Email", Email);
            }

            if (_config.ConnectionStrings.ConnectionStrings["last"] != null)
            {
                _config.ConnectionStrings.ConnectionStrings["last"].ConnectionString = ConnectionStringBuilder.ConnectionString;
            }
            else
            {
                _config.ConnectionStrings.ConnectionStrings.Add(new ConnectionStringSettings("last", ConnectionStringBuilder.ConnectionString));
            }
            try
            {
                _config.Save(ConfigurationSaveMode.Minimal);
            }
            catch (Exception)
            {
                // silence
            }
        }

        /// <summary>
        /// Connect to SQL Server and fill in the GUI to provide options for the check
        /// </summary>
        public void Connect()
        {
            using (var cn = new SqlConnection(ConnectionStringBuilder.ConnectionString))
            {
                try
                {
                    cn.Open();
                }
                catch (SqlException)
                {
                    MessageBox.Show("NOT connected. Error in connection properties ?", "not connected", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                Databases.Clear();

                var qry = $"SELECT Name, compatibility_level FROM sys.databases WHERE database_id > 5 ORDER BY name; {_infos.Query}";
                using (var cmd = new SqlCommand(qry, cn))
                {
                    SqlDataReader rd;
                    try
                    {
                        rd = cmd.ExecuteReader();
                    }
                    catch (SqlException e)
                    {
                        var msg = $"SQL Error in query ({qry}).\nError {e.Number} : {e.Message}";
                        MessageBox.Show(msg, "SQL Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        SimpleLog.Error(msg);
                        return;
                    }
                    while (rd.Read())
                    {
                        Databases.Add(new SqlServer.Database()
                        {
                            Checked = false,
                            Name = rd.GetString(0),
                            CompatibilityLevel = (SqlServer.DatabaseCompatibilityLevel)rd.GetByte(1) // danger ! TryParse ??
                        });
                    }

                    rd.NextResult();
                    _infos.Set(rd);
                    rd.Close();
                    ServerInfos = $"SQL Server version {_infos.ProductVersion}, Edition {_infos.Edition}";
                    OnPropertyChanged("ServerInfos");
                }
                cn.Close();
                // OutputPath
                OutputPath = $@"{_outputRoot}{ConnectionStringBuilder.DataSource.Replace("\\", ".")}\";
                Directory.CreateDirectory(OutputPath);

                // we need to change the output path of the manager
                _ce.OutputPath = OutputPath;

                SimpleLog.SetLogFile(logDir:OutputPath, check:false);
                SimpleLog.Info(_infos.ToString());

                OnPropertyChanged("Databases");

                // status
                StatusText = $"Connected to {ConnectionStringBuilder.DataSource} ({_infos.MachineName}), SQL Server {_infos.ProductVersion} {_infos.Edition}";
                OnPropertyChanged("StatusText");
            }

        }

        /// <summary>
        /// runs full analysis. Call the background worker to run asynchronously
        /// </summary>
        public void RunFullAnalysis()
        {
            if (ConnectionStringBuilder?.DataSource == null || Databases.Count == 0)
            {
                MessageBox.Show("you need to connect to SQL Server first", "Connection", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
                //throw new InvalidOperationException("you need to connect to SQL Server first");
            }
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                _worker.RunWorkerAsync();
            }
            catch (InvalidOperationException) // when we call it again while the backround thread is already working
            {
                Mouse.OverrideCursor = null;
            }

        }

        /// <summary>
        /// sends the full analysis result by email
        /// </summary>
        public void SendResultsByEmail()
        {
            var zipfile = ZipResults();
            var filename = Path.GetTempPath() + "\\mymessage.eml";

            if (String.IsNullOrWhiteSpace(Email))
            {
                MessageBox.Show("You need to enter a valid email address", "email error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //var mail = new Tools.Mapi();
            //mail.AddRecip(Email, Email, false);
            //mail.Attach(filename);
            //mail.Send("coucou", "text");

            var from = System.Security.Principal.WindowsIdentity.GetCurrent()?.Name ?? "";
            // for now :
            from = Email;

            try
            {
                var mailMessage = new MailMessage(from, Email)
                {
                    Subject = "SQL Trismegiste analysis result for " + ConnectionStringBuilder.DataSource,
                    IsBodyHtml = true,
                    Body = "<span style='font-size: 12pt; color: red;'>Please find result attached</span>"
                };

                mailMessage.Attachments.Add(new Attachment(zipfile));

                //save the MailMessage to the filesystem
                mailMessage.Save(filename);

                //Open the file with the default associated application registered on the local machine
                Process.Start(filename);
            }
            catch (Exception e)
            {
                MessageBox.Show($"Error in sending email. Error : {e.Message}", "email error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        /// <summary>
        /// creates a zip file from the analysis result folder to be attached in the results email
        /// </summary>
        /// <returns></returns>
        public string ZipResults()
        {
            var zipPath =
                $@"{Path.GetTempPath()}\sqltrismegiste.{ConnectionStringBuilder.DataSource.Replace("\\", ".")
                    }.{DateTime.Now.ToString("yyyy-MM-dd-h-mm")}.zip";

            if (File.Exists(zipPath)) File.Delete(zipPath);
            ZipFile.CreateFromDirectory(_outputRoot, zipPath);

            return zipPath;
        }

        /// <summary>
        /// Select all databases in the databases list to be checked
        /// </summary>
        public void CheckAllDatabases()
        {
            foreach (var db in Databases)
            {
                db.Checked = true;
            }
            OnPropertyChanged("Databases");
        }
        #endregion
    }
}
