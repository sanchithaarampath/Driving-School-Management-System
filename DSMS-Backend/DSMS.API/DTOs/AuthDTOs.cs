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
        public int? BranchId { get; set; }
        public bool FirstTimeLogin { get; set; }
    }

    public class ChangePasswordDto
    {
        public int UserId { get; set; }
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class SetPasswordDto
    {
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

    // ==================== EMPLOYEE ====================
    public class EmployeeCreateDto
    {
        public int BranchId { get; set; }
        public int? UserId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string Nic { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Designation { get; set; }
        public string? Department { get; set; }
        public string? JoinDate { get; set; }
        public string? Address { get; set; }
        public string? EmergencyContact { get; set; }
    }

    public class EmployeeUpdateDto
    {
        public int BranchId { get; set; }
        public int? UserId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Designation { get; set; }
        public string? Department { get; set; }
        public string? JoinDate { get; set; }
        public string? Address { get; set; }
        public string? EmergencyContact { get; set; }
    }

    // ==================== USER MANAGEMENT ====================
    public class CreateUserDto
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string UserFullName { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public int? BranchId { get; set; }
    }

    public class UpdateUserDto
    {
        public string UserFullName { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public int? BranchId { get; set; }
        public bool Active { get; set; }
    }

    // ==================== STUDENT (EXTENDED) ====================
    public class StudentCreateExtDto
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
        public string? PostalCode { get; set; }
        public string? ExistingLicenseNo { get; set; }
        public string? PackageType { get; set; }
        public bool? IsSpecialRequirements { get; set; }
        public int? SpecialRequirementTypeId { get; set; }
        public bool? HasBirthCertificate { get; set; }
        public bool? HasNtmiMedical { get; set; }
        public bool? HasNicCopy { get; set; }
        public int? CoursePackageId { get; set; }
        public List<string> VehicleClasses { get; set; } = new();
    }

    // ==================== TRAINING ATTENDANCE ====================
    public class TrainingAttendanceCreateDto
    {
        public int StudentPackageRegistrationId { get; set; }
        public int? InstructorId { get; set; }
        public string AttendanceDate { get; set; } = string.Empty;
        public int DayNumber { get; set; }
        public string? Notes { get; set; }
        public bool IsReadyForPracticalTest { get; set; }
    }

    // ==================== EXAM RESULT ====================
    public class ExamResultUpdateDto
    {
        public int StudentPackageRegistrationId { get; set; }
        public string ExamStatus { get; set; } = string.Empty; // "Pass" | "Fail"
        public string? ExamDate { get; set; }
    }

    // ==================== BILLING ====================
    public class BillCreateDto
    {
        public int StudentId { get; set; }
        public decimal PackagePrice { get; set; }   // student's registered package price
        public decimal InstallmentAmount { get; set; } // amount paid in THIS instalment
        public decimal DiscountAmount { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
        public string? ReferenceNo { get; set; }
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

    // ==================== PAYMENT GATEWAY ====================
    public class StripeIntentDto
    {
        public decimal Amount { get; set; }
        public int BillId { get; set; }
        public string? StudentName { get; set; }
    }

    // ==================== COURSE PACKAGE ====================
    public class CoursePackageDto
    {
        public string PackageName { get; set; } = string.Empty;
        public string CourseType { get; set; } = "Full Course";
        public string VehicleClassCodes { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal MaxDiscount { get; set; }
        public string? Description { get; set; }
    }

    // ==================== PAYHERE ====================
    public class PayHereCreateDto
    {
        public decimal Amount { get; set; }
        public string OrderId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
    }

    // ==================== RECEIPT / NOTIFICATIONS ====================
    public class SendReceiptDto
    {
        public bool SendEmail { get; set; }
        public bool SendWhatsApp { get; set; }
        public string? OverrideEmail { get; set; }
        public string? OverridePhone { get; set; }
    }
}