using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace chatbot.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        
        public string Question { get; set; }
        public string ImageBase64 { get; set; }
        public string Answer { get; set; }
        public DateTime Timestamp { get; set; }

        public int ChatSessionId { get; set; }
        public virtual ChatSession ChatSession { get; set; }
    }
}