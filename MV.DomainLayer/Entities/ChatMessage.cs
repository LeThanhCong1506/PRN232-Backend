namespace MV.DomainLayer.Entities;

public class ChatMessage
{
    public int MessageId { get; set; }

    /// <summary>
    /// UserId của người gửi
    /// </summary>
    public int SenderId { get; set; }

    /// <summary>
    /// UserId của người nhận (null = gửi cho Admin/Store)
    /// </summary>
    public int? ReceiverId { get; set; }

    public string Content { get; set; } = null!;

    /// <summary>
    /// True nếu tin nhắn từ Admin/Staff
    /// </summary>
    public bool IsFromAdmin { get; set; }

    public DateTime SentAt { get; set; } = DateTime.Now;

    public bool IsRead { get; set; } = false;

    public virtual User Sender { get; set; } = null!;

    public virtual User? Receiver { get; set; }
}
