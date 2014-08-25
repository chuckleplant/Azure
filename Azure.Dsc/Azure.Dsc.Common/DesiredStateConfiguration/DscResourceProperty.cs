using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Sogeti.IaC.Model.DesiredStateConfiguration;

namespace Sogeti.IaC.Model.DesiredStateConfiguration
{
    public class DscResourceProperty
    {
        private DscResourcePropertyValue _value = new DscResourcePropertyValue();
        public DscResourceProperty()
        {
            
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public string Name { get; set; }
        public string Type { get; set; }
        public DscResourcePropertyAttribute Attribute { get; set; }
        public string[] ValidationSet { get; set; }
        public string EmbeddedInstance { get; set; }

        public DscResourcePropertyValue Value
        {
            get
            {
                if (Name.EndsWith("[]"))
                {
                    if (!string.IsNullOrEmpty(EmbeddedInstance))
                    {
                        //TODO: Bind Complex Property Value...
                    }
                    else
                    {
                        _value = new DscResourcePropertySimpleArrayValue();
                    }
                }
                else
                {
                    if (EmbeddedInstance == "MSFT_Credential")
                    {
                        _value = new DscResourcePropertyCredentialValue();
                    }
                }

                return _value;
            }
            set { _value = value; }
        } 
    }
}
