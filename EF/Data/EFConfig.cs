using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using CommLiby.Database.EF;

namespace DBUtility.EF.Data
{
    [AutoDbSet]
    public class EFConfig : ModelBase<EFConfig>
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }

        [Required]
        public string key { get; set; }

        public string value { get; set; }

        public DateTime uptime { get; set; }
    }
}
