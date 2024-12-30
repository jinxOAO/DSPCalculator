using DSPCalculator.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSPCalculator
{
    public static class Utils
    {
        public static string KMG(double num, int decimalNum = 2)
        {
            string tail = "";
            if (num < 1000)
            {
                return string.Format("{0:F2}", num);
            }
            else if (num < 1000000)
            {
                tail = "k";
                num /= 1000;
            }
            else if (num < 1000000000)
            {
                tail = "M";
                num /= 1000000;
            }
            else if (num < 1000000000000L)
            {
                tail = "G";
                num /= 1000000000;
            }
            else if (num < 1000000000000000L)
            {
                tail = "T";
                num /= 1000000000000L;
            }
            else if (num < 1000000000000000000L)
            {
                tail = "P";
                num /= 1000000000000000L;
            }
            else
            {
                tail = "E";
                num /= 1000000000000000000L;
            }


            if(num < 9.995)
            {
                return string.Format("{0:F3} {1}", num, tail);
            }
            else if (num < 99.95)
            {
                return string.Format("{0:F2} {1}", num, tail);
            }
            else if (num < 999.5)
            {
                return string.Format("{0:F1} {1}", num, tail);
            }
            else
            {
                return string.Format("{0:N0} {1}", num, tail);
            }
        }

        public static string KMG(int num)
        {
            if (num < 1000)
            {
                return num.ToString();
            }
            else
            {
                return KMG((double)num);
            }
            
        }

        public static double GetIncMilli(int index, UserPreference preference)
        {
            if (preference.customizeIncMilli && index > 0)
                return preference.incMilliOverride;
            else
                return Cargo.incTableMilli[index];
        }

        public static double GetAccMilli(int index, UserPreference preference)
        {
            if(preference.customizeAccMilli && index > 0)
                return preference.accMilliOverride;
            else
                return Cargo.accTableMilli[index];
        }

        public static double GetPowerRatio(int index, UserPreference preference)
        {
            return Cargo.powerTableRatio[index];
        }
    }
}
