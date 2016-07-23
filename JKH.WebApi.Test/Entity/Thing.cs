using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JKH.WebApi.Test.Entity
{
    public class Thing
    {
        [Key]
        public int Id { get; set; }
        [Required, StringLength(256)]
        public string Name { get; set; }
        public string Description { get; set; }
        [Timestamp]
        public byte[] RowVersion { get; set; }
    }
}
