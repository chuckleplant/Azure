using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sogeti.IaC.Model.DesiredStateConfiguration
{
    public class DscResourceClass
    {
        public DscResourceClass()
        {
            Properties = new Collection<DscResourceProperty>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool IsOmiBaseResource { get; set; }
        public Collection<DscResourceProperty> Properties { get; set; }
    }
}
