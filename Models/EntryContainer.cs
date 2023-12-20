using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Titan.Models
{
    public struct EntryContainer(List<(long, double)> VelocityEntries, List<(long, double)> PositionEntries, List<(long, double)> VoltageEntries)
    {
        /// <summary>
        /// Entries that correspond to motor velocity
        /// </summary>
        public readonly List<(long, double)> VelocityEntries = VelocityEntries;

        /// <summary>
        /// Entries that correspond to motor position
        /// </summary>
        public readonly List<(long, double)> PositionEntries = PositionEntries;

        /// <summary>
        /// Entries that correspond to motor voltage
        /// </summary>
        public readonly List<(long, double)> VoltageEntries = VoltageEntries;
    }
}
