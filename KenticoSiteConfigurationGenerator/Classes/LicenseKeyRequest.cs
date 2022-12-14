using KenticoSiteConfigurationGenerator.com.kentico.service;

namespace KenticoSiteConfigurationGenerator
{

    public class LicenseKeyRequest
    {
        public LicenseKeyRequest(LicenseKeyTypeEnum licenseType, string domain)
        {
            LicenseType = licenseType;
            Domain = domain;
        }

        public LicenseKeyTypeEnum LicenseType { get; set; }
        public string Domain { get; set; }
    }

}
