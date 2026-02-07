using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MV.DomainLayer.DTOs.Login.Request
{
    public class UpdateUserDto
    {
        [Required]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = null;

        [Required]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "Số điện thoại phải đúng 10 chữ số")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Số điện thoại chỉ được chứa số")]
        public string? Phone { get; set; }

        public string? Address { get; set; }
    }
}
