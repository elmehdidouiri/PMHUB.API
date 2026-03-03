using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMHUB.Domain.Enums
{
    public enum ProcessStatus
    {
        NotStarted,
        AsIsProcessUnderstanding,
        ToBeProcessDefinition,
        OnHold,
        Completed,
        ImplementedInPDMlink,
        Cancelled
    }
}
