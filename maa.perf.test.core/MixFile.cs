using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace maa.perf.test.core
{
    public class ApiInfo
    {
        public Api ApiName { get; set; }
        public double Percentage { get; set; }
    }
    public class MixFile
    {
        public List<ApiInfo> ApiMix { get; set; }
    }
}
