using System;
using System.Data.SqlClient;
using System.Windows;
using static SQLTrismegiste.SqlServer.DataReaderExtensions;

namespace SQLTrismegiste.SqlServer
{
    public class ServerInfo
    {
        public string ServerName { get; set; }
        public string InstanceName { get; set; }
        public int VersionMajor { get; private set; }
        public int VersionMinor { get; private set; }
        public int BuildNumber { get; private set; }
        public string ProductVersion { get; private set; }
        public string ProductLevel { get; private set; }
        public string Edition { get; private set; }
        public int EngineEdition { get; private set; }
        public string ComputerNamePhysicalNetBIOS { get; private set; }
        public string MachineName { get; private set; }
        public DateTime StartTime { get; private set; }

        public string Query => @"
                SELECT
                (@@microsoftversion / 0x1000000) & 0xff AS VersionMajor,
                (@@microsoftversion / 0x10000) & 0xff AS VersionMinor,
                @@microsoftversion & 0xffff AS BuildNumber,
                SERVERPROPERTY('ProductVersion') AS ProductVersion,
                SERVERPROPERTY('ProductLevel') AS ProductLevel,
                SERVERPROPERTY('Edition') AS Edition,
                SERVERPROPERTY('EngineEdition') AS EngineEdition,
                SERVERPROPERTY('ComputerNamePhysicalNetBIOS') AS ComputerNamePhysicalNetBIOS,
                SERVERPROPERTY('InstanceName') AS InstanceName,
                SERVERPROPERTY('ServerName') AS ServerName,
                SERVERPROPERTY('MachineName') AS MachineName,
                (SELECT login_time FROM sysprocesses WHERE spid = 1) as StartTime;           
            ";

        public void Set(SqlDataReader reader)
        {
            // SMO being deprecated, doing it manually
            reader.Read();

            try
            {
                VersionMajor = reader.GetInt32(0);
                VersionMinor = reader.GetInt32(1);
                BuildNumber = reader.GetInt32(2);
                ProductVersion = reader.GetString(3);
                ProductLevel = reader.GetString(4);
                Edition = reader.GetString(5);
                EngineEdition = reader.GetInt32(6);
                ComputerNamePhysicalNetBIOS = reader.GetString(7) ?? "NULL";
                InstanceName = (reader.GetStringOrNull(8) ?? "NULL");
                ServerName = (reader.GetString(9) ?? "NULL");
                MachineName = (reader.GetStringOrNull(10) ?? "NULL");
                StartTime = reader.GetDateTime(11);
            }
            catch (Exception e)
            {
                var msg =
                    $"{App.Localized["msgErrorInGettingInformation"]} : {e.Message}";
                MessageBox.Show(msg, "ServerInfo error", MessageBoxButton.OK, MessageBoxImage.Error);
                SimpleLogger.SimpleLog.Error(msg);
            }
        }

        public override string ToString()
        {
            return $"ServerName = {ServerName};VersionMajor={VersionMajor};VersionMinor={VersionMinor};BuildNumber={BuildNumber};ProductVersion={ProductVersion};"
                   +
                   $"ProductLevel={ProductLevel};Edition={Edition};EngineEdition={EngineEdition};ComputerNamePhysicalNetBIOS={ComputerNamePhysicalNetBIOS};StatTime={StartTime}";
        }
    }
}
