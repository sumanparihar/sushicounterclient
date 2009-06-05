using System;
using System.Collections.Generic;
using System.Text;

namespace SushiLibrary.Validation
{
    public static class ValidationRule
    {
        /// <summary>
        /// Verify that number of whole months between the start and end date is equal to the specified number of months
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="months"></param>
        /// <returns></returns>
        public static bool CheckDuration(DateTime start, DateTime end, int months)
        {
            if (end.Month - start.Month == months)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks that the start day is the first day of the month
        /// </summary>
        /// <param name="?"></param>
        /// <param name="?"></param>
        /// <returns></returns>
        public static bool CheckStartDay(DateTime start)
        {
            if (start.Day == 1)
            {
                return true;
            }

            return false;
        }


        /// <summary>
        /// Checks that the end day is the last day of the month
        /// </summary>
        /// <param name="?"></param>
        /// <param name="?"></param>
        /// <returns></returns>
        public static bool CheckEndDay(DateTime end)
        {
            if (end.Day == DateTime.DaysInMonth(end.Year, end.Month))
            {
                return true;
            }

            return false;
        }
    }
}
