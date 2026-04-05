namespace DSMS.API.DTOs
{
    // ==================== AUTH ====================
    public class LoginRequestDto
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string UserFullName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int UserId { get; set; }
        public int RoleId { get; set; }
        public bool FirstTimeLogin { get; set; }
    }

    public class ChangePasswordDto
    {
        public int UserId { get; set; }
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    // ==================== STUDENT ====================
    public class StudentCreateDto
    {
        public int BranchId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string? WhatsAppNumber { get; set; }
        public string Address { get; set; } = string.Empty;
        public string Nic { get; set; } = string.Empty;
        public string Dob { get; set; } = string.Empty;
        public string? Gender { get; set; }
        public string? NearestPoliceStation { get; set; }
        public string? NearestDivisionalSecretariat { get; set; }
        public string? ExistingLicenseNo { get; set; }
        public bool? IsSpecialRequirements { get; set; }
        public int? SpecialRequirementTypeId { get; set; }
    }

    public class StudentUpdateDto
    {
        public string StudentName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string? WhatsAppNumber { get; set; }
        public string Address { get; set; } = string.Empty;
        public string? Gender { get; set; }
        public string? NearestPoliceStation { get; set; }
        public string? NearestDivisionalSecretariat { get; set; }
        public string? ExistingLicenseNo { get; set; }
        public bool? IsSpecialRequirements { get; set; }
        public int? SpecialRequirementTypeId { get; set; }
    }

    // ==================== BILLING ====================
    public class BillCreateDto
    {
        public int StudentId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public string? Remarks { get; set; }
    }

    public class PaymentCreateDto
    {
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
        public string? ReferenceNo { get; set; }
        public string? Remarks { get; set; }
        public int StudentId { get; set; }
    }
}