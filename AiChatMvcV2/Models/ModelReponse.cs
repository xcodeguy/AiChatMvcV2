using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AiChatMvcV2.Models
{
    public class ModelReponse
    {
        public string? Response { get; set; } = null;
        public bool? Done { get; set; } = true;
        public string? DoneReason { get; set; } = null;
        public string? Context { get; set; } = null;
    }
}