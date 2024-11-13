using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace efcore.batchload.data.Entities
{
    [Table("#TempIdTable")]
    public class TempIdTable
    {
        public int PrivateId { get; internal set; }

        public int RefId { get; internal set; }

        public override string ToString() => $"[{PrivateId}] {RefId}";
    }
}
