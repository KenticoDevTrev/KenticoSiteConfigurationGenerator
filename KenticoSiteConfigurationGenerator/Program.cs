using System;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.IO;
using System.Linq;
using WebServiceDemo.Classes;

namespace KenticoSiteConfigurationGenerator
{
    /// <summary>
    /// WebService demo program
    /// </summary>
    public partial class Program
    {
        /* BEGIN EDIT */

        /// <summary>
        /// License Key Generation required values
        /// </summary>
        public const string serialNumber = "CV13-000-000-000-0000-00000000-C00000A000";
        public const string clientPortalUsername = "mylogin@mysite.com";

        /// <summary>
        /// Controls how the Local Dev and Dev Urls are generated
        /// </summary>
        public const DevUrlMode localDevUrlMode = DevUrlMode.WildcardDomain;
        public const DevUrlMode devUrlMode = DevUrlMode.Normal;
        public const string localDevDomain = "mycompanytesting.com";

        /// <summary>
        /// Used for IIS Binding Powershel script generation
        /// </summary>
        public const string localDevAdminIISSiteName = "localadmin.thesite.com";
        public const string localDevMVCIISSiteName = ""; // empty = iis express
        public const string devAdminIISSiteName = "devadmin-kentico";
        public const string devMVCIISSiteName = "dev-mvc";
        public const string stagingAdminIISSiteName = "stagingadmin-kentico";
        public const string stagingMVCIISSiteName = "staging-kentico";
        public const string productionAdminIISSiteName = "admin-kentico";
        public const string productionMVCIISSiteName = "kentico";

        /// <summary>
        /// Determines what operations you wish to run
        /// </summary>
        public const bool GenerateLicenseKeys = true;
        public const bool GenerateSiteAndDomainAliasScripts = true;
        public const bool GenerateIISBindingPowershell = true;
        public const bool GenerateIISExpressAppHostConfigBindings = true;

        /* END BEGIN EDIT */

        /// <summary>
        /// EDIT ME!
        /// </summary>
        /// <returns></returns>
        private static IEnumerable<BaseConfiguration> GetBaseConfigurations()
        {
            var configurations = new List<BaseConfiguration>();

            // BEGIN EDIT: Paste in Scripts from DomainGenerator column A
            configurations.Add(new BaseConfiguration("mysite", "", "com", false, "MySite", SSLCertType.Wildcard));
            configurations.Add(new BaseConfiguration("mysite", "services", "com", false, "MyServiceSite", SSLCertType.Wildcard));
            configurations.Add(new BaseConfiguration("othersite", "", "com", false, "MyOtherSite", SSLCertType.DomainSpecific));
            configurations.Add(new BaseConfiguration("othersite", "", "uk", false, "MyOtherSiteUK", SSLCertType.DomainSpecific));
            // END EDIT

            return configurations;
        }

        /// <summary>
        /// Main function
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            var environmentToAdminAndMVCIISSiteName = new Dictionary<EnvironmentType, Tuple<string, string>>();
            if (!string.IsNullOrWhiteSpace(localDevAdminIISSiteName) || !string.IsNullOrWhiteSpace(localDevMVCIISSiteName))
            {
                environmentToAdminAndMVCIISSiteName.Add(EnvironmentType.LocalDev, new Tuple<string, string>(localDevAdminIISSiteName, localDevMVCIISSiteName));
            }
            if (!string.IsNullOrWhiteSpace(devAdminIISSiteName) || !string.IsNullOrWhiteSpace(devMVCIISSiteName))
            {
                environmentToAdminAndMVCIISSiteName.Add(EnvironmentType.Dev, new Tuple<string, string>(devAdminIISSiteName, devMVCIISSiteName));
            }
            if (!string.IsNullOrWhiteSpace(stagingAdminIISSiteName) || !string.IsNullOrWhiteSpace(stagingMVCIISSiteName))
            {
                environmentToAdminAndMVCIISSiteName.Add(EnvironmentType.Staging, new Tuple<string, string>(stagingAdminIISSiteName, stagingMVCIISSiteName));
            }
            if (!string.IsNullOrWhiteSpace(productionAdminIISSiteName) || !string.IsNullOrWhiteSpace(productionMVCIISSiteName))
            {
                environmentToAdminAndMVCIISSiteName.Add(EnvironmentType.Production, new Tuple<string, string>(productionAdminIISSiteName, productionMVCIISSiteName));
            }

            var baseConfigurations = GetBaseConfigurations().ToList();
            // Set local dev domain and modes
            baseConfigurations.ForEach(x =>
            {
                x.LocalDevDomain = localDevDomain;
                x.LocalDevUrlMode = localDevUrlMode;
                x.DevUrlMode = devUrlMode;
            });

            if (GenerateLicenseKeys)
            {
                // Generates Site Licenses
                GenerateLicenses(baseConfigurations, serialNumber, clientPortalUsername);
            }

            if (GenerateSiteAndDomainAliasScripts)
            {
                // Generates the CMS_SIte and CMS_SiteDomainAlias scripts
                GenerateSiteConfiguration(baseConfigurations);
            }

            if (GenerateIISBindingPowershell)
            {
                // Get powershell IIS Bindings for the various environments
                GenerateIISBindingPowershells(baseConfigurations, environmentToAdminAndMVCIISSiteName);
            }

            if (GenerateIISExpressAppHostConfigBindings)
            {
                // For Using IIS Express, you can take the outputed value and add it to your .vs/SITENAME/config/applicationhost.config configuration/system.applicationHost/sites/site[name='theMVCSite']/bindings 
                GenerateLocalDevIISExpressBindings(baseConfigurations);
            }
        }

        private static void GenerateLicenses(IEnumerable<BaseConfiguration> baseConfigurations, string kenticoSerialNumber, string kenticoClientUserName)
        {
            var requests = baseConfigurations.SelectMany(x => x.GetLicenseKeyRequests());
            var results = new List<LicenseKeyResult>();

            string errorMessage = null;

            foreach (var request in requests)
            {

                // For older .NET than 4.6 you need to opt-in to using TLS1.2 by uncommenting following line
                // ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                // Get license key
                string licenseKey = GetLicenseKey(kenticoSerialNumber, request.Domain, kenticoClientUserName, out errorMessage);

                if (licenseKey == "")
                {
                    // Something is wrong
                    results.Add(new LicenseKeyResult(request.Domain, errorMessage));
                }
                else
                {
                    // Parse the DB values
                    string lookupVal = "EXPIRATION:";
                    string expiration = licenseKey.Substring(licenseKey.IndexOf(lookupVal) + lookupVal.Length, 8);
                    expiration = $"{expiration.Substring(4, 2).Trim('0')}/{expiration.Substring(6, 2).Trim('0')}/{expiration.Substring(0, 4)}";

                    // SErver
                    string serverLookup = "SERVERS:";
                    string serverVal = licenseKey.Substring(licenseKey.IndexOf(serverLookup) + serverLookup.Length, 4);
                    bool whitespaceFound = false;
                    string servers = "";
                    for (int i = 0; i < serverVal.Length && !whitespaceFound; i++)
                    {
                        if (int.TryParse(serverVal[i].ToString(), out var num))
                        {
                            servers += num.ToString();
                        }
                        else
                        {
                            whitespaceFound = true;
                        }
                    }

                    string productLookup = "PRODUCT:";
                    string productVal = licenseKey.Substring(licenseKey.IndexOf(productLookup) + productLookup.Length, 4);
                    bool productWhitespaceFound = false;
                    string product = "";
                    for (int i = 0; i < productVal.Length && !productWhitespaceFound; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(productVal[i].ToString()))
                        {
                            product += productVal[i];
                        }
                        else
                        {
                            productWhitespaceFound = true;
                        }
                    }
                    string edition = "";
                    if (product.StartsWith("C") && (product.EndsWith("12") || product.EndsWith("13")))
                    {
                        edition = product.Substring(1, 1);
                    }
                    else
                    {
                        edition = "V";
                    }

                    results.Add(new LicenseKeyResult(request.Domain, licenseKey, edition, expiration, int.Parse(servers)));
                    // OK
                }
            }

            // WRite results

            File.WriteAllText("../../GeneratedResults\\KenticoLicensesSQL\\licensekeys.sql", string.Join("", results.Where(x => !x.Errored).Select(x => x.ToSQLDeleteInsert())));
            if (results.Any(x => x.Errored))
            {
                File.WriteAllText("errors.txt", string.Join("", results.Where(x => x.Errored).Select(x => x.ToSQLDeleteInsert())));
            }
        }

        /// <summary>
        /// Return license key
        /// </summary>
        /// <param name="sn">Serial number</param>
        /// <param name="domain">Domain</param>
        /// <param name="userName">User name</param>
        /// <param name="errorMessage">Output parameter with possible error message</param>
        private static string GetLicenseKey(string sn, string domain, string userName, out string errorMessage)
        {
            RSACryptoServiceProvider rcp = new RSACryptoServiceProvider();
            rcp.FromXmlString("<RSAKeyValue><Modulus>4yUuUVYKw0lQDTMONy356ufkOgSUjeGdP168JdNAQbGnaqSuXek/qe0HztzUteY4oWR73CimGNshL9viCcmc/AZhWoLUdiML1rii6Rup7KRXY4azti65cmgADeFXkO3Cl2dmyQaYX6IN+VHTTjp1B3SSdqv2dbz0VFwjZuVG/1DK9avlnQkS04W5UAGNR3ZDfqBJaw7Fou/7X2psH6S0xXVV+qy64qgJcfe3OkyH+zqUCEf6hOJwBeGNXc3NWw629UatPg7cgvLvj/JSDfuNmUKrVkC40GaLXkAuPUZiyledyEb3a/G2D8YjG48Xk4qxz1vtBd+EsIaiNez2iVx5Dw==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>");

            com.kentico.service.CMSLicenseService cmsLic = new com.kentico.service.CMSLicenseService();

            // Encrypted serial, domain and username
            string data = Convert.ToBase64String(rcp.Encrypt(ASCIIEncoding.ASCII.GetBytes(sn + "|" + domain + "|" + userName), false));

            // If version is not set directly, key will be the same version as the serial number
            int? version = null;

            // Different types of keys - Main will use up a slot of the license, other types can be used only for unlimited licenses
            com.kentico.service.LicenseKeyTypeEnum keyType = com.kentico.service.LicenseKeyTypeEnum.Main;

            return cmsLic.GetKeyGeneral(data, version, keyType, out errorMessage);
        }

        private static void GenerateSiteConfiguration(IEnumerable<BaseConfiguration> baseConfigurations)
        {
            var configurations = baseConfigurations.Select(x => x.GetSiteConfiguration());

            File.WriteAllText("../../GeneratedResults\\SiteConfigurationSQL\\CMSSiteDomainAlias.sql", $"DECLARE @SiteID int = 0;\n\r{string.Join("\n\r", configurations.SelectMany(x => x.GetCMSSiteDomainAliasScript()))}");

            File.WriteAllText("../../GeneratedResults\\SiteConfigurationSQL\\CMSSiteLocalDev.sql", string.Join("\n\r", configurations.Select(x => x.GetCMSSiteSqlScript(EnvironmentType.LocalDev))));
            File.WriteAllText("../../GeneratedResults\\SiteConfigurationSQL\\CMSSiteDev.sql", string.Join("\n\r", configurations.Select(x => x.GetCMSSiteSqlScript(EnvironmentType.Dev))));
            File.WriteAllText("../../GeneratedResults\\SiteConfigurationSQL\\CMSSiteStaging.sql", string.Join("\n\r", configurations.Select(x => x.GetCMSSiteSqlScript(EnvironmentType.Staging))));
            File.WriteAllText("../../GeneratedResults\\SiteConfigurationSQL\\CMSSiteProduction.sql", string.Join("\n\r", configurations.Select(x => x.GetCMSSiteSqlScript(EnvironmentType.Production))));
        }

        private static void GenerateIISBindingPowershells(IEnumerable<BaseConfiguration> baseConfigurations, Dictionary<EnvironmentType, Tuple<string, string>> environmentToAdminAndMVCIISSiteNames)
        {
            var configurations = baseConfigurations.Select(x => new Tuple<SiteConfiguration, Dictionary<EnvironmentType, SSLCertType>>(x.GetSiteConfiguration(), x.GetSSLCertTypePerEnvironment()));

            if (environmentToAdminAndMVCIISSiteNames.ContainsKey(EnvironmentType.LocalDev))
            {
                File.WriteAllText("../../GeneratedResults\\IISBindingsPowershell\\IISBindings-LocalDev.ps1", string.Join("\n\r", configurations.Select(x =>
                    x.Item1.GetIISBindingPowershellScript(EnvironmentType.LocalDev, environmentToAdminAndMVCIISSiteNames[EnvironmentType.LocalDev].Item1, environmentToAdminAndMVCIISSiteNames[EnvironmentType.LocalDev].Item2, x.Item2[EnvironmentType.LocalDev]))
                ));
            }
            if (environmentToAdminAndMVCIISSiteNames.ContainsKey(EnvironmentType.Dev))
            {

                File.WriteAllText("../../GeneratedResults\\IISBindingsPowershell\\IISBindings-Dev.ps1", string.Join("\n\r", configurations.Select(x =>
                x.Item1.GetIISBindingPowershellScript(EnvironmentType.Dev, environmentToAdminAndMVCIISSiteNames[EnvironmentType.Dev].Item1, environmentToAdminAndMVCIISSiteNames[EnvironmentType.Dev].Item2, x.Item2[EnvironmentType.Dev]))
            ));
            }
            if (environmentToAdminAndMVCIISSiteNames.ContainsKey(EnvironmentType.Staging))
            {

                File.WriteAllText("../../GeneratedResults\\IISBindingsPowershell\\IISBindings-Staging.ps1", string.Join("\n\r", configurations.Select(x =>
                x.Item1.GetIISBindingPowershellScript(EnvironmentType.Staging, environmentToAdminAndMVCIISSiteNames[EnvironmentType.Staging].Item1, environmentToAdminAndMVCIISSiteNames[EnvironmentType.Staging].Item2, x.Item2[EnvironmentType.Staging]))
            ));
            }
            if (environmentToAdminAndMVCIISSiteNames.ContainsKey(EnvironmentType.Production))
            {

                File.WriteAllText("../../GeneratedResults\\IISBindingsPowershell\\IISBindings-Production.ps1", string.Join("\n\r", configurations.Select(x =>
                x.Item1.GetIISBindingPowershellScript(EnvironmentType.Production, environmentToAdminAndMVCIISSiteNames[EnvironmentType.Production].Item1, environmentToAdminAndMVCIISSiteNames[EnvironmentType.Production].Item2, x.Item2[EnvironmentType.Production]))
            ));
            }

            // Write Powershell for Bindings
            string powerShell = @"<#
.Synopsis
   Adds binding to IIS Site, handling getting the SSL Cert if provided and checking if it already exists.
.DESCRIPTION
   Adds binding to IIS Site, handling getting the SSL Cert if provided and checking if it already exists.
.EXAMPLE
   .\IISBindingHandler.ps1 -IISSiteName ""admin.testsite.com"" -BindingDomain ""devadmintestsite.mycompanysite.com"" -CertSubjectMatch ""*.mycompanysite.com""
#>

[CmdletBinding()]
[Alias()]
Param
(
    [Parameter(Mandatory=$true,
                ValueFromPipelineByPropertyName=$true,
                Position=0)]
    [string]$IISSiteName,

    # BindingDomain
    [Parameter(Mandatory=$true,
                ValueFromPipelineByPropertyName=$true,
                Position=1)]
    [string]$BindingDomain,

    # CertMatch
    [Parameter(Mandatory=$false,
                ValueFromPipelineByPropertyName=$true,
                Position=2)]
    [string]$CertSubjectMatch
)

Begin{
 $ErrorActionPreference = 'Stop';
 $bindOnSSL = $false;
 $thumbprint = """";
 $httpsBindingAdded = $false;
 $httpBindingAdded = $false;
    if (-NOT([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] ""Administrator"")) {
        Write-Error ""Administrator priviliges are required. Please restart this script with elevated rights.""
    }
    if($CertSubjectMatch -ne $null -and $CertSubjectMatch -ne """") {
        $bindOnSSL = $true;
        $thumbprint = Get-ChildItem -Path Cert:\LocalMachine\My | Where-Object {$_.Subject -Match [System.Text.RegularExpressions.Regex]::Escape($CertSubjectMatch)} | Select-Object Thumbprint
        if($thumbprint -eq $null) {
            Write-Error ""Could not find a SSL Cert for $($CertSubjectMatch), aborting SSL Cert insert for $($BindingDomain)"";
        }
    }
}
Process
{
    if($bindOnSSL -eq $true) {
        if($null -eq (Get-WebBinding -Name $IISSiteName -HostHeader $BindingDomain -Port 443 -Protocol ""https"")) {
            New-WebBinding -Name $IISSiteName -IPAddress ""*"" -Port 443 -HostHeader $BindingDomain -Protocol ""https"" -SslFlags 1
            (Get-WebBinding -Name $IISSiteName -HostHeader $BindingDomain -Port 443 -Protocol ""https"").AddSslCertificate($thumbprint.Thumbprint, ""my"")
            $httpsBindingAdded = $true;
        } 
    }
    if($null -eq (Get-WebBinding -Name $IISSiteName -HostHeader $BindingDomain -Port 80 -Protocol ""http"")) {
            New-WebBinding -Name $IISSiteName -IPAddress ""*"" -Port 80 -HostHeader $BindingDomain -Protocol ""http""
            $httpBindingAdded = $true;
    } 
}
End
{
    echo ""Operation for $($BindingDomain) complete: $(If($bindOnSSL -eq $true) { If($httpsBindingAdded -eq $true) { ""Https Binding Added""} Else {""Https Binding Already Present""} } Else { ""(Https Skipped, no CertSubjectMatch given)"" }), $(If($httpBindingAdded -eq $true) { ""Http Binding Added."" } Else {""Http Binding Already Present.""})"";
}";
            File.WriteAllText("../../GeneratedResults\\IISBindingsPowershell\\IISBindingHandler.ps1", powerShell);



        }

        private static void GenerateLocalDevIISExpressBindings(IEnumerable<BaseConfiguration> baseConfigurations)
        {
            var siteConfigs = baseConfigurations.Select(x => x.GetSiteConfiguration());
            File.WriteAllText("../../GeneratedResults\\LocalDevIISBindings\\LocalDev-IISExpressBindings.txt", string.Join("\n\r", siteConfigs.Select(x => x.GetLocalDevIISExpressBindings())));
        }
    }
}
