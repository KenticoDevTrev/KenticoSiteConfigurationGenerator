using System;

namespace KenticoSiteConfigurationGenerator
{

    public class LicenseKeyResult
    {
        public LicenseKeyResult(string licenseDomain, string licenseKey, string licenseEdition, string expiration, int licenseServers)
        {
            LicenseDomain = licenseDomain;
            LicenseKey = licenseKey;
            LicenseEdition = licenseEdition;
            Expiration = expiration;
            LicenseServers = licenseServers;
            Errored = false;
        }
        public LicenseKeyResult(string licenseDomain, string error)
        {
            Error = error;
            Errored = false;
        }
        public bool Errored { get; internal set; }
        public string LicenseDomain { get; set; }
        public string LicenseKey { get; set; }
        public string LicenseEdition { get; set; }
        public string Expiration { get; set; }
        public int LicenseServers { get; set; }
        public string Error { get; }

        public string ToSQLDeleteInsert()
        {
            if (Errored)
            {
                return $"--ERROR: {LicenseDomain}: {string.Join("\n\r--", Error.Split("\n\r".ToCharArray(), StringSplitOptions.RemoveEmptyEntries))}";
            }
            return $"DELETE FROM CMS_LicenseKey where LicenseDomain = '{LicenseDomain}'\n\r" +
                $"INSERT INTO CMS_LicenseKey ([LicenseDomain] ,[LicenseKey] ,[LicenseEdition] ,[LicenseExpiration] ,[LicenseServers]) VALUES ('{LicenseDomain}' ,'{LicenseKey}','{LicenseEdition}','{Expiration}' ,{LicenseServers})\n\r\n\r";
        }
    }
}
