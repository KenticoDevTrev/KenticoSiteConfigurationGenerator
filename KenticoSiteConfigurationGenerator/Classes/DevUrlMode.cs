using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebServiceDemo.Classes
{
    public enum DevUrlMode
    {
        /// <summary>
        /// If using LocalHost for admin/mvc dev environment
        /// </summary>
        Localhost,
        /// <summary>
        /// puts "dev" and "localdev" prefixing the domain (ex "dev.thesite.com", "devadmin.thesite.com", "localdev.thesite.com", "localdevadmin.thesite.com")
        /// </summary>
        Normal,
        /// <summary>
        /// if using a global wildcard for all dev sites (ex "mycompanytest.com", postfixes appropriate domains with this wildcard (ex mysite.mycompanytest.com, adminmysite.mycompanytest.com, devmysite.companytest.com, devadmin.mycompanytest.com)
        /// </summary>
        WildcardDomain
    }
}
