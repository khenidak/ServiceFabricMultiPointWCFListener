using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WcfServiceLib
{
    // WCF Service Sample
    // this a tradtional service that will be migrated to Service Fabric as is
    // it does not use Service Fabric state management
    public class WcfService : SvcInterface1, SvcInterface2
    {
        public string SvcInterface1Op1(string stringParam)
        {
            return string.Concat("SvcInterface1Op1", " ", stringParam);
        }

        public string SvcInterface1Op2(string stringParam)
        {
            return string.Concat("SvcInterface1Op2", " ", stringParam);
        }

        public string SvcInterface2Op1(string stringParam)
        {
            return string.Concat("SvcInterface2Op1", " ", stringParam);
        }

        public string SvcInterface2Op2(string stringParam)
        {
            return string.Concat("SvcInterface2Op2", " ", stringParam);
        }
    }
}
