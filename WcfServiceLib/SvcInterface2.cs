using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace WcfServiceLib
{

    // WCF Service Interface Sample


    [ServiceContract]
    public  interface SvcInterface2
    {
        [OperationContract]
        string SvcInterface2Op1(string stringParam);
        [OperationContract]
        string SvcInterface2Op2(string stringParam);
    }
}
