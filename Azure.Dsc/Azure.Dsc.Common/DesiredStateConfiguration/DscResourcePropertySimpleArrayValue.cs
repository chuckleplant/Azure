using System.Collections.ObjectModel;
using System.Text;

namespace Sogeti.IaC.Model.DesiredStateConfiguration
{
    public class DscResourcePropertySimpleArrayValue : DscResourcePropertyValue
    {
        public DscResourcePropertySimpleArrayValue()
        {
            Values = new Collection<string>();
        }

        public override string Value
        {
            get
            {
                StringBuilder builder = new StringBuilder();
                foreach (var value in Values)
                {
                    var stringValue = string.Format(@"""{0}""", value);
                    builder.Append(stringValue);
                    builder.Append(", ");
                }
                
                return string.Format(@"@({0})", builder.ToString().TrimEnd(',', ' '));
            }
        }
        public Collection<string> Values { get; set; } 
        
    }
}
