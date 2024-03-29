﻿/*
Copyright (c) 2009, Serials Solutions
All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
    * Neither the name of the <ORGANIZATION> nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace SushiLibrary
{
    public static class ReportLoader
    {
        private static string GetValue(XmlNode node)
        {
            if (node == null)
            {
                return null;
            }
            return node.Value;
        }

        private static string GetInnerText(XmlNode node)
        {
            if (node == null)
            {
                return null;
            }
            return node.InnerText;
        }

        public static SushiReport LoadCounterReport(XmlDocument reportXml)
        {
            SushiReport sushiReport = new SushiReport();

            XmlNamespaceManager xmlnsManager = new XmlNamespaceManager(reportXml.NameTable);

            xmlnsManager.AddNamespace("c", "http://www.niso.org/schemas/counter");
            xmlnsManager.AddNamespace("s", "http://www.niso.org/schemas/sushi");

            sushiReport.ReportType =
                (CounterReportType)
                Enum.Parse(typeof (CounterReportType),
                           GetValue(
                               reportXml.SelectSingleNode("//s:ReportDefinition", xmlnsManager).Attributes["Name"]),
                           true);

            sushiReport.Release = GetValue(reportXml.SelectSingleNode("//s:ReportDefinition", xmlnsManager).Attributes["Release"]);

            XmlNodeList reports = reportXml.SelectNodes("//c:Report", xmlnsManager);

            if (reports != null)
            {
                sushiReport.CounterReports = new List<CounterReport>();

                foreach (XmlNode report in reports)
                {
                    DateTime created;
                    DateTime.TryParse(GetValue(report.Attributes["Created"]), out created);

                    var counterReport = new CounterReport();


                    counterReport.ID = GetValue(report.Attributes["ID"]);
                    counterReport.Name = GetValue(report.Attributes["Name"]);
                    counterReport.Title = GetValue(report.Attributes["Title"]);
                    counterReport.Version = GetValue(report.Attributes["Version"]);
                    counterReport.Created = created;
                    counterReport.Vendor_Id = GetInnerText(report.SelectSingleNode("c:Vendor/c:ID", xmlnsManager));
                    counterReport.Vendor_Name = GetInnerText(report.SelectSingleNode("c:Vendor/c:Name", xmlnsManager));
                    counterReport.Vendor_ContactEmail =
                        GetInnerText(report.SelectSingleNode("c:Vendor/c:Contact/c:E-mail", xmlnsManager));
                    counterReport.Vendor_WebSiteUrl =
                        GetInnerText(report.SelectSingleNode("c:Vendor/c:WebSiteUrl", xmlnsManager));
                    counterReport.Vendor_LogoUrl = GetInnerText(report.SelectSingleNode("c:Vendor/c:LogoUrl", xmlnsManager));
                    counterReport.Customer_ID = GetInnerText(report.SelectSingleNode("c:Customer/c:ID", xmlnsManager));
                    counterReport.Customer_Name = GetInnerText(report.SelectSingleNode("c:Customer/c:Name", xmlnsManager));
                    counterReport.Customer_Consortium_Code =
                        GetInnerText(report.SelectSingleNode("c:Customer/c:Consortium/c:Code", xmlnsManager));
                    counterReport.Customer_Consortium_WellKnownName =
                        GetInnerText(report.SelectSingleNode("c:Customer/c:Consortium/c:WellKnownName", xmlnsManager));


                    XmlNodeList reportItems = report.SelectNodes("c:Customer/c:ReportItems", xmlnsManager);

                    if (reportItems != null)
                    {
                        counterReport.ReportItems = new List<CounterReportItem>();

                        foreach (XmlNode reportItem in reportItems)
                        {
                            CounterReportItem counterReportItem;

                            switch (sushiReport.ReportType)
                            {
                                case CounterReportType.JR1:
                                case CounterReportType.JR2:
                                case CounterReportType.JR3:
                                case CounterReportType.JR4:
                                case CounterReportType.JR5:
                                    JournalReportItem journalReportItem = new JournalReportItem();
                                    XmlNodeList identifiers = reportItem.SelectNodes("c:ItemIdentifier", xmlnsManager);

                                    if (identifiers != null)
                                    {
                                        foreach (XmlNode identifier in identifiers)
                                        {
                                            string value = identifier.SelectSingleNode("c:Value", xmlnsManager).InnerText;
                                            switch (identifier.SelectSingleNode("c:Type", xmlnsManager).InnerText.ToLower())
                                            {
                                                // see http://www.niso.org/workrooms/sushi/values/#item
                                                case "issn":
                                                    journalReportItem.PrintISSN = value;
                                                    break;
                                                case "print_issn":
                                                    journalReportItem.PrintISSN = value;
                                                    break;
                                                case "online_issn":
                                                    journalReportItem.OnlineISSN = value;
                                                    break;
                                            }
                                        }
                                    }

                                    counterReportItem = journalReportItem;
                                    break;
                                default:
                                    counterReportItem = new CounterReportItem();
                                    break;
                            }


                            counterReportItem.Name = reportItem.SelectSingleNode("c:ItemName", xmlnsManager).InnerText;
                            counterReportItem.Publisher = reportItem.SelectSingleNode("c:ItemPublisher", xmlnsManager).InnerText;
                            counterReportItem.Platform = reportItem.SelectSingleNode("c:ItemPlatform", xmlnsManager).InnerText;

                            XmlNodeList metrics = reportItem.SelectNodes("c:ItemPerformance", xmlnsManager);

                            if (metrics != null)
                            {
                                foreach (XmlNode metric in metrics)
                                {
                                    DateTime start, end;
                                    DateTime.TryParse(metric.SelectSingleNode("c:Period/c:Begin", xmlnsManager).InnerText, out start);
                                    DateTime.TryParse(metric.SelectSingleNode("c:Period/c:End", xmlnsManager).InnerText, out end);

                                    CounterMetricCategory category = CounterMetricCategory.Invalid;

                                    try
                                    {
                                        category = (CounterMetricCategory)Enum.Parse(typeof(CounterMetricCategory), metric.SelectSingleNode("c:Category", xmlnsManager).InnerText, true);
                                    }
                                    catch (ArgumentException ex)
                                    {
                                        Console.WriteLine(string.Format("WARNING - Found Invalid Metric Category Type: {0}", metric.SelectSingleNode("c:Category", xmlnsManager).InnerText));
                                    }
                                    CounterMetric counterMetric = counterReportItem.GetMetric(start, end, category);

                                    XmlNodeList instances = metric.SelectNodes("c:Instance", xmlnsManager);

                                    if (instances != null)
                                    {
                                        if (counterMetric.Instances == null)
                                        {
                                            counterMetric.Instances = new List<CounterMetricInstance>();
                                        }

                                        foreach (XmlNode instance in instances)
                                        {
                                            CounterMetricInstance metricInstance= new CounterMetricInstance();
                                            metricInstance.Type =
                                                (CounterMetricType)
                                                Enum.Parse(typeof (CounterMetricType),
                                                           instance.SelectSingleNode("c:MetricType",
                                                                                   xmlnsManager).InnerText, true);

                                            // return exception if can't parse count, since it's important to process properly
                                            metricInstance.Count = Int32.Parse(instance.SelectSingleNode("c:Count", xmlnsManager).InnerText); ;

                                            counterMetric.Instances.Add(metricInstance);
                                        }

                                    }

                                }
                            }

                            counterReport.ReportItems.Add(counterReportItem);

                        }
                    }

                    sushiReport.CounterReports.Add(counterReport);
                }
            }


            return sushiReport;
        }

    }
}
