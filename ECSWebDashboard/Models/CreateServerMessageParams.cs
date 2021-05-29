using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ECSWebDashboard.Models
{
    public class CreateServerMessageParams
    {
        public string Name { get; set; }
        public string Content { get; set; }
        public string ChannelID { get; set; }
    }
}
