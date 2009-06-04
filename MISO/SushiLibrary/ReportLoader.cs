using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace SushiLibrary
{
    public static class ReportLoader
    {
        private static string GetNodeValue(XmlNode attribute)
        {
            if (attribute == null)
            {
                return null;
            }
            return attribute.Value;
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
                           GetNodeValue(
                               reportXml.SelectSingleNode("//s:ReportDefinition", xmlnsManager).Attributes["Name"]),
                           true);

            sushiReport.Release = GetNodeValue(reportXml.SelectSingleNode("//s:ReportDefinition", xmlnsManager).Attributes["Release"]);

            XmlNodeList reports = reportXml.SelectNodes("//c:Report", xmlnsManager);

            if (reports != null)
            {
                sushiReport.CounterReports = new List<CounterReport>();

                foreach (XmlNode report in reports)
                {
                    CounterReport counterReport = new CounterReport();
                    counterReport.ID = GetNodeValue(report.Attributes["ID"]);
                    counterReport.Name = GetNodeValue(report.Attributes["Name"]);
                    counterReport.Title = GetNodeValue(report.Attributes["Title"]);
                    counterReport.Version = GetNodeValue(report.Attributes["Version"]);
                    DateTime created;
                    DateTime.TryParse(GetNodeValue(report.Attributes["Created"]), out created);
                    counterReport.Created = created;

                    // TODO add the rest of report metadata

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

                                    string category = metric.SelectSingleNode("c:Category", xmlnsManager).InnerText;
                                    CounterMetric counterMetric = counterReportItem.GetMetric(start, end, (CounterMetricCategory)Enum.Parse(typeof(CounterMetricCategory), category, true));

                                    XmlNodeList instances = metric.SelectNodes("c:Instance", xmlnsManager);

                                    if (instances != null)
                                    {
                                        if (counterMetric.Instance == null)
                                        {
                                            counterMetric.Instance = new List<CounterMetricInstance>();
                                        }

                                        foreach (XmlNode instance in instances)
                                        {
                                            CounterMetricInstance metricInstance= new CounterMetricInstance();
                                            metricInstance.Type =
                                                (CounterMetricType)
                                                Enum.Parse(typeof (CounterMetricType),
                                                           instance.SelectSingleNode("c:MetricType",
                                                                                   xmlnsManager).InnerText, true);

                                            int count;
                                            Int32.TryParse(
                                                instance.SelectSingleNode("c:Count", xmlnsManager).InnerText,
                                                out count);
                                            metricInstance.Count = count;

                                            counterMetric.Instance.Add(metricInstance);
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
