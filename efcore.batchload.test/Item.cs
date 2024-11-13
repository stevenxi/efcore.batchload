using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace efcore.batchload.test
{
    [Table("Items")]
    internal class Item
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; }
    }
}
