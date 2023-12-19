using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Titan.DataConverters
{
    public struct FriendlyRecord<T>(int Id, string Name)
    {
        public int Id = Id;
        public string Name = Name;
        public List<T> Values = new();
    }
}
