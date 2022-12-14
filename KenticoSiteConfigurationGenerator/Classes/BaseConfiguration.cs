using System;
using System.Collections.Generic;
using KenticoSiteConfigurationGenerator.com.kentico.service;
using WebServiceDemo.Classes;

namespace KenticoSiteConfigurationGenerator
{

    public class BaseConfiguration
    {
        public BaseConfiguration(string baseDomainName, string prefix, string postFix, bool postFixIsPartOfDomainIdentity, string cMSSiteCodeName, SSLCertType iISSertType)
        {
            BaseDomainName = baseDomainName;
            Prefix = prefix;
            PostFix = postFix;
            PostFixIsPartOfDomainIdentity = postFixIsPartOfDomainIdentity;
            CMSSiteCodeName = cMSSiteCodeName;
            IISCertType = iISSertType;
        }

        /// <summary>
        /// The base domain name, if for example "service.mysite.com" then the base domain name is just "mysite"
        /// </summary>
        public string BaseDomainName { get; set; }

        /// <summary>
        /// The prefix of the site (exclude www), such as "service.mysite.com" would have a prefix of "service"
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// The postfix of the site, usually "com"
        /// </summary>
        public string PostFix { get; set; }

        /// <summary>
        /// If true, then the postfix helps differentiate itself from the other domains.  Ex if you have "mysite.it" and "mysite.es", the postfix it or es are two different sites.  Vs. "mysite.com" and "someothersite.com", the com doesn't differentiate itself.
        /// </summary>
        public bool PostFixIsPartOfDomainIdentity { get; set; }

        /// <summary>
        /// The code name that matches for this site.
        /// </summary>
        public string CMSSiteCodeName { get; set; }

        public SSLCertType IISCertType { get; set; }

        public string LocalDevDomain { get; set; }
        public DevUrlMode LocalDevUrlMode { get; set; } = DevUrlMode.Localhost;
        public DevUrlMode DevUrlMode { get; set; } = DevUrlMode.Localhost;

        private Tuple<string, string> GetDevAdminAndMVCUrls(DevUrlMode devUrlMode)
        {
            switch (devUrlMode)
            {
                case DevUrlMode.Localhost:
                default:
                    return new Tuple<string, string>(string.Empty, string.Empty);
                case DevUrlMode.Normal:
                    // Local dev
                    return new Tuple<string, string>($"localdevadmin{Prefix}.{BaseDomainName}.{PostFix}", $"localdev{Prefix}.{BaseDomainName}.{PostFix}");
                case DevUrlMode.WildcardDomain:
                    return new Tuple<string, string>($"devadmin{Prefix}{BaseDomainName}{(PostFixIsPartOfDomainIdentity ? PostFix : "")}.{LocalDevDomain}", $"dev{Prefix}{BaseDomainName}{(PostFixIsPartOfDomainIdentity ? PostFix : "")}.{LocalDevDomain}");
            }
        }


        public IEnumerable<LicenseKeyRequest> GetLicenseKeyRequests()
        {
            var requests = new List<LicenseKeyRequest>();

            // Local dev
            var localDevAdminMVC = GetDevAdminAndMVCUrls(LocalDevUrlMode);
            if (!string.IsNullOrWhiteSpace(localDevAdminMVC.Item1) && !string.IsNullOrWhiteSpace(localDevAdminMVC.Item2))
            {
                requests.Add(new LicenseKeyRequest(LicenseKeyTypeEnum.DevTest, localDevAdminMVC.Item1));
                requests.Add(new LicenseKeyRequest(LicenseKeyTypeEnum.DevTest, localDevAdminMVC.Item2));
            }
            var devAdminMVC = GetDevAdminAndMVCUrls(DevUrlMode);
            if (!string.IsNullOrWhiteSpace(devAdminMVC.Item1) && !string.IsNullOrWhiteSpace(devAdminMVC.Item2))
            {
                requests.Add(new LicenseKeyRequest(LicenseKeyTypeEnum.DevTest, devAdminMVC.Item1));
                requests.Add(new LicenseKeyRequest(LicenseKeyTypeEnum.DevTest, devAdminMVC.Item2));
            }

            // staging
            requests.Add(new LicenseKeyRequest(LicenseKeyTypeEnum.Staging, $"staging{Prefix}.{BaseDomainName}.{PostFix}"));
            requests.Add(new LicenseKeyRequest(LicenseKeyTypeEnum.Staging, $"stagingadmin{Prefix}.{BaseDomainName}.{PostFix}"));
            // production
            requests.Add(new LicenseKeyRequest(LicenseKeyTypeEnum.Main, $"{(!string.IsNullOrWhiteSpace(Prefix) ? Prefix + "." : "")}{BaseDomainName}.{PostFix}"));
            requests.Add(new LicenseKeyRequest(LicenseKeyTypeEnum.Main, $"admin{Prefix}.{BaseDomainName}.{PostFix}"));
            return requests;
        }

        public SiteConfiguration GetSiteConfiguration()
        {
            var configuration = new SiteConfiguration(CMSSiteCodeName);

            // Local dev
            var localDevAdminMVC = GetDevAdminAndMVCUrls(LocalDevUrlMode);
            if (!string.IsNullOrWhiteSpace(localDevAdminMVC.Item1) && !string.IsNullOrWhiteSpace(localDevAdminMVC.Item2))
            {
                configuration.EnvironmentToAdminPresentationDomains.Add(EnvironmentType.LocalDev, new Tuple<string, string>(localDevAdminMVC.Item1, $"https://{localDevAdminMVC.Item2}"));
            }

            // Shared dev
            var devAdminMVC = GetDevAdminAndMVCUrls(DevUrlMode);
            if (!string.IsNullOrWhiteSpace(devAdminMVC.Item1) && !string.IsNullOrWhiteSpace(devAdminMVC.Item2))
            {
                configuration.EnvironmentToAdminPresentationDomains.Add(EnvironmentType.Dev, new Tuple<string, string>(devAdminMVC.Item1, $"https://{devAdminMVC.Item2}"));
            }

            // staging
            configuration.EnvironmentToAdminPresentationDomains.Add(EnvironmentType.Staging, new Tuple<string, string>($"stagingadmin{Prefix}.{BaseDomainName}.{PostFix}", $"https://staging{Prefix}.{BaseDomainName}.{PostFix}"));
            
            // production, use direct url for presentation
            configuration.EnvironmentToAdminPresentationDomains.Add(EnvironmentType.Production, new Tuple<string, string>($"admin{Prefix}.{BaseDomainName}.{PostFix}", $"https://{(!string.IsNullOrWhiteSpace(Prefix) ? Prefix : "www")}.{BaseDomainName}.{PostFix}"));

            // add alias of main site + www variation
            if (string.IsNullOrWhiteSpace(Prefix))
            {
                configuration.AliasDomains.Add($"https://www.{BaseDomainName}.{PostFix}");
                configuration.AliasDomains.Add($"https://{BaseDomainName}.{PostFix}");
            }
            else
            {
                configuration.AliasDomains.Add($"https://{Prefix}.{BaseDomainName}.{PostFix}");
            }

            return configuration;
        }

        public Dictionary<EnvironmentType, SSLCertType> GetSSLCertTypePerEnvironment()
        {
            var dictionary = new Dictionary<EnvironmentType, SSLCertType>();
            dictionary.Add(EnvironmentType.LocalDev, SSLCertType.Wildcard);
            dictionary.Add(EnvironmentType.Dev, SSLCertType.Wildcard);
            dictionary.Add(EnvironmentType.Staging, IISCertType);
            dictionary.Add(EnvironmentType.Production, IISCertType);
            return dictionary;
        }
    }
}
