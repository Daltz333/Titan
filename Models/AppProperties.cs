using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Titan.Utilities;

namespace Titan.Models
{
    public static class AppProperties
    {
        private readonly static StringBuilder builder = new();

        public static string AppBuildInformation
        {
            get
            {
                builder.Clear();

                builder.Append("Build: ");
                builder.AppendLine(Assembly.GetExecutingAssembly().GetLinkerTime().ToString("yyyy-MM-dd HH:mm"));

                builder.Append("Version: ");
                builder.AppendLine(FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion);

                return builder.ToString();
            }
        }


    }
}
