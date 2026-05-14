namespace GwaFind.Models
{
    public class Message
    {
        public int Id { get; set; }
        public int InquiryId { get; set; }
        public Inquiry Inquiry { get; set; } = null!;
        public string SenderId { get; set; } = "";
        public ApplicationUser Sender { get; set; } = null!;
        public string Content { get; set; } = "";
        public DateTime SentAt { get; set; } = DateTime.Now;
        public bool IsRead { get; set; } = false;
    }
}