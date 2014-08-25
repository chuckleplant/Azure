using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sogeti.IaC.Model.DesiredStateConfiguration
{
    public enum DscResourcePropertyAttribute
    {
        Key = 0,
        Required = 1,
        Read = 2,
        Write = 3
    }
}
