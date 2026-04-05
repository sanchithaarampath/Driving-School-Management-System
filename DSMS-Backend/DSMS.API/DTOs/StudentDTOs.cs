namespace DSMS.API.DTOs
{
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
}