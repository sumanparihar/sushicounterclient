using System;
using System.Collections.Generic;
using System.Text;

namespace SushiLibrary.Validation
{
    public class CounterValidator : SushiValidator
    {
        public override bool Validate(SushiReport report)
        {
            base.Validate(report);

            if (report.CounterReports.Count < 1)
            {
                // no reports to validate, fail validation and quit
                _errorMessages.Add("No Counter Data found.");
                return false;
            }

            // for now just expect one counter report
            CounterReport counterReport = report.CounterReports[0];

            foreach (var reportItem in counterReport.ReportItems)
            {
                foreach (var key in reportItem.Metrics.Keys)
                {
                    CounterMetric metric = reportItem.Metrics[key];

                    if (!ValidationRule.CheckStartDay(metric.Start))
                    {
                        _isValid = false;
                        _errorMessages.Add(
                            string.Format(
                                "Report Item \"{0}\" has a metric start date that is not the first day of the month. The start date given was {1}.",
                                reportItem.Name, metric.Start.ToString("yyyy-M-d")));
                    }

                    if (!ValidationRule.CheckEndDay(metric.End))
                    {
                        _isValid = false;
                        _errorMessages.Add(
                            string.Format(
                                "Report Item \"{0}\" has a metric end date that is not the last day of the month. The end date given was {1}.",
                                reportItem.Name, metric.End.ToString("yyyy-M-d")));
                    }

                    switch (report.ReportType)
                    {
                        case CounterReportType.JR1:
                        case CounterReportType.JR2:
                        case CounterReportType.JR3:
                        case CounterReportType.JR4:
                        case CounterReportType.DB1:
                        case CounterReportType.DB2:
                        case CounterReportType.DB3:
                            if (!ValidationRule.CheckDuration(metric.Start, metric.End, 0))
                            {
                                _isValid = false;
                                _errorMessages.Add(
                                    string.Format(
                                        "Report Item \"{0}\" has a metric duration of more than 1 month. The given dates were from {1} to {2}.",
                                        reportItem.Name, metric.Start.ToString("yyyy-M-d"), metric.End.ToString("yyyy-M-d")));
                            }
                            break;
                    }
                }
            }



            return _isValid;
        }
    }
}
