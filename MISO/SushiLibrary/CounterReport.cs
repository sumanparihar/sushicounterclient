using System;
using System.Collections.Generic;
using System.Text;

namespace SushiLibrary
{
    public class CounterReport
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Version { get; set; }
        public DateTime Created { get; set; }

        public string Vendor_Id { get; set; }
        public string Vendor_Name { get; set; }
        public string Vendor_ContactEmail { get; set; }
        public string Vendor_WebSiteUrl { get; set; }
        public string Vendor_LogoUrl { get; set; }
        public string Customer_ID { get; set; }
        public string Customer_Name { get; set; }
        public string Customer_Consortium_Code { get; set; }
        public string Customer_Consortium_WellKnownName { get; set; }

        public List<CounterReportItem> CounterReportItems { get; set; }


    }
}
