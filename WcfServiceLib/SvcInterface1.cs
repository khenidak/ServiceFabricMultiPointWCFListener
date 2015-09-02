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
    public interface SvcInterface1
    {
        [OperationContract]
        string SvcInterface1Op1(string stringParam);
        [OperationContract]
        string SvcInterface1Op2(string stringParam);


    }
}
