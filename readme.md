# Instructions
1. Open the KenticoSiteConfigurationGenerator solution
2. Edit the public consts and enter in the applicable data, and what operations you wish to run.
3. Open the DomainGenerator.xlsx and add in any new or remaining domains
4. Use the first columns to generate the C# Code (copy it) and paste it into KenticoSiteConfigurationGenerator/Program.cs -> GetBaseConfigurations() in the edit section
5. Run the program, the generated files will appear in the KenticoSiteConfigurationGenerator/GeneratedResults folders

For License Keys or Site Configurations, you'll need to either run the SQL script on the database (backup first)

For IIS Bindings, you'll need to run the IISBindings-ENVIRONMENT.ps1 in admin mode at the location of these files (cd C:\Path\To\GeneratedResults\IISBIndingsPowershell)

For IIS Express, you'll want to edit your .vs/[MVC]/config/applicationhost.config and put the generated bindings in the configuration/system.applicationHost/sites/site[@name="thesitename"]/bindings next to the default bindings

# IIS Express and https
IIS Express supports HTTPS bindings for localhost, but in order to use a domain, you'll need to configure IIS Express to use a wildcard certificate and your dev url must be a variant of that (ex thesite.companytesting.com, where *.companytesting.com is a cert you have).

Get the wildcard cert .pfx file, then follow the following instructions:

Based on this [article](https://dillieodigital.wordpress.com/2016/04/12/soup-to-nuts-custom-domains-and-ssl-in-iis-express/) (with a slight modification), you can set it up:

1. Install the *.hbstest.net SSL Cert on your machine (run -> certmgr.msc -> Personal -> Certificates -> Import)	
1. Double click on the *.thewildcardcert.com cert, go to Details -> Thumbprint -> Copy value
1. Run powershell in admin mode

```ps	
netsh http show sslcert > sslcert.txt
```

1. Open the sslcert.txt and look for the Application ID, copy the value (including curly brackets)
1. Run the powershell below, replacing `{yourapplicationid}` and `thewildcardthumbprintvalue` with the above values

```ps
netsh <enter>	
http delete sslcert ipport=0.0.0.0:443
http add sslcert ipport=0.0.0.0:443 appid={yourapplicationid} certhash=thewildcardthumbprintvalue 
```

NOTE: when copying, hidden characters often copy as well, causing "invalid parameter" error, so may have to manually type or delete around the beginning / end
	
Now IIS express on any port 443 will use the *.thewildcardcert.com ssl cert, so just use that for your testing urls