using System;
using System.Collections.Generic;
using System.Linq;

namespace KenticoSiteConfigurationGenerator
{

    public class SiteConfiguration
    {
        public SiteConfiguration(string siteName)
        {
            SiteName = siteName;
        }

        public string SiteName { get; set; }
        public string PresentationDomain { get; set; }
        public string AdminDomain { get; set; }
        public Dictionary<EnvironmentType, Tuple<string, string>> EnvironmentToAdminPresentationDomains { get; set; } = new Dictionary<EnvironmentType, Tuple<string, string>>();
        public List<string> AliasDomains { get; set; } = new List<string>();
        public Dictionary<EnvironmentType, List<Tuple<string, string>>> AlternativeAdminPresentationPairing = new Dictionary<EnvironmentType, List<Tuple<string, string>>>();

        public string GetCMSSiteSqlScript(EnvironmentType environmentType)
        {
            var adminAndPresentation = EnvironmentToAdminPresentationDomains[environmentType];
            return $"update CMS_Site set SiteDomainName = '{adminAndPresentation.Item1}', SitePresentationURL = '{adminAndPresentation.Item2}', SiteLastModified = GETDATE() where SiteName = '{SiteName}'\n\r";
        }

        public string GetIISBindingPowershellScript(EnvironmentType environmentType, string adminIISSiteName, string mvcIISSiteName, SSLCertType certType)
        {
            var adminAndPresentation = new List<Tuple<string, string>>() { EnvironmentToAdminPresentationDomains[environmentType] };
            if (AlternativeAdminPresentationPairing.ContainsKey(environmentType))
            {
                adminAndPresentation.AddRange(AlternativeAdminPresentationPairing[environmentType]);
            }

            string commands = string.Join("\n\r", adminAndPresentation.Select(x =>
                (string.IsNullOrWhiteSpace(adminIISSiteName) ? "" : $".\\IISBindingHandler.ps1 -IISSiteName \"{adminIISSiteName}\" -BindingDomain \"{x.Item1}\" {(certType == SSLCertType.None ? "" : $"-CertSubjectMatch \"{(certType == SSLCertType.Wildcard ? GetWildcard(x.Item1) : x.Item1)}\"")} \n\r") +
                (string.IsNullOrWhiteSpace(mvcIISSiteName) ? "" : $".\\IISBindingHandler.ps1 -IISSiteName \"{mvcIISSiteName}\" -BindingDomain \"{x.Item2.Replace("https://", "").Replace("http://", "")}\"  {(certType == SSLCertType.None ? "" : $"-CertSubjectMatch \"{(certType == SSLCertType.Wildcard ? GetWildcard(x.Item1) : x.Item1)}\"")} \n\r")
            ));

            // Add aliases on the MVC Site only on production
            if (environmentType == EnvironmentType.Production)
            {
                commands += (string.IsNullOrWhiteSpace(mvcIISSiteName) ? "" : string.Join("\n\r", AliasDomains.Select(x => $".\\IISBindingHandler.ps1 -IISSiteName \"{mvcIISSiteName}\" -BindingDomain \"{x.Replace("https://", "").Replace("http://", "")}\" {(certType == SSLCertType.None ? "" : $"-CertSubjectMatch \"{(certType == SSLCertType.Wildcard ? GetWildcard(x) : x)}\"")}\"\n\r")));
            }
            return commands;
        }

        public string GetWildcard(string domain)
        {
            var splitDomain = domain.Split(".".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            if (splitDomain.Length >= 2)
            {
                return $"*.{splitDomain[splitDomain.Length - 2]}.{splitDomain[splitDomain.Length - 1]}";
            }
            else
            {
                return "*." + domain;
            }
        }

        public IEnumerable<string> GetCMSSiteDomainAliasScript()
        {
            List<string> scripts = new List<string>();
            scripts.Add($"SET @SiteID = (select top 1 S.SiteID from CMS_SIte S where SiteName = '{SiteName}');\r\nDelete from CMS_SiteDomainAlias where SiteID = @SiteID\n\r");
            // Add admin - presentation combos
            scripts.AddRange(EnvironmentToAdminPresentationDomains.Values.Union(AlternativeAdminPresentationPairing.Values.SelectMany(x => x)).Select(x =>
                 $"insert into CMS_SiteDomainAlias\r\n  ([SiteDomainAliasName],[SiteID],[SiteDefaultVisitorCulture],[SiteDomainGUID],[SiteDomainLastModified],[SiteDomainPresentationUrl],[SiteDomainAliasType]) VALUES\r\n  ('{x.Item1}',@SiteID ,NULL,NEWID(),GETDATE(),'{x.Item2}',0)\n\r"
                )
            );
            // Add aliases
            scripts.AddRange(AliasDomains.Select(x =>
                $"insert into CMS_SiteDomainAlias\r\n  ([SiteDomainAliasName],[SiteID],[SiteDefaultVisitorCulture],[SiteDomainGUID],[SiteDomainLastModified],[SiteDomainPresentationUrl],[SiteDomainAliasType]) VALUES\r\n  (NULL,@SiteID ,NULL,NEWID(),GETDATE(),'{x}',1)\n\r"
                )
            );

            return scripts;
        }

        public string GetLocalDevIISExpressBindings()
        {
            var localDevEnv = EnvironmentToAdminPresentationDomains[EnvironmentType.LocalDev];
            return $"<binding protocol=\"http\" bindingInformation=\"*:80:{localDevEnv.Item2.Replace("https://", "").Replace("http://", "")}\" />\r\n" +
                $"<binding protocol=\"https\" bindingInformation=\"*:443:{localDevEnv.Item2.Replace("https://", "").Replace("http://", "")}\" />";
        }
    }
}
