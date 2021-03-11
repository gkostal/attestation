using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace maa.perf.test.core.Model
{
    public interface IWeightedObject
    {
        public double Weight { get; set; }

        public double Percentage { get; set; }
    }
}
