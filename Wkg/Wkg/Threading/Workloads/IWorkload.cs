using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wkg.Threading.Workloads.Queuing;

namespace Wkg.Threading.Workloads;

internal interface IWorkload
{
    bool TryInternalBindQdisc(IQdisc qdisc);
}
