using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DSMS.API.Data;
using DSMS.API.Models;
using DSMS.API.Helpers;

namespace DSMS.API.Controllers
{
    [ApiController]
    [Route("api/student-document")]
    [Authorize]
    public class StudentDocumentController : ControllerBase
    {
        private readonly DsmsDbContext _context;
        private readonly IWebHostEnvironment _env;

        private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "BirthCertificate", "NtmiMedical", "NicCopy"
        };

        private static readonly HashSet<string> AllowedMimes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/jpg", "image/webp", "application/pdf"
        };

        public StudentDocumentController(DsmsDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ── GET /api/student-document/{studentId} ─────────────────────────────
        [HttpGet("{studentId:int}")]
        public async Task<IActionResult> GetForStudent(int studentId)
        {
            var docs = await _context.StudentDocuments
                .Where(d => d.StudentId == studentId && d.Active == true)
                .OrderBy(d => d.DocumentType)
                .ThenByDescending(d => d.CreatedDateTime)
                .Select(d => new
                {
                    d.Id,
                    d.StudentId,
                    d.DocumentType,
                    d.FileName,
                    d.ContentType,
                    d.CreatedDateTime
                })
                .ToListAsync();

            return Ok(docs);
        }

        // ── POST /api/student-document/upload ─────────────────────────────────
        // Form fields: studentId (int), documentType (string), file (IFormFile)
        [HttpPost("upload")]
        [Authorize(Roles = "Company Admin,Admin,Branch Admin,Staff")]
        [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB
        public async Task<IActionResult> Upload(
            [FromForm] int studentId,
            [FromForm] string documentType,
            IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file provided." });

            if (!AllowedTypes.Contains(documentType))
                return BadRequest(new { message = "Invalid document type. Use BirthCertificate, NtmiMedical, or NicCopy." });

            if (!AllowedMimes.Contains(file.ContentType))
                return BadRequest(new { message = "Only JPG, PNG, WebP, and PDF files are allowed." });

            if (file.Length > 10 * 1024 * 1024)
                return BadRequest(new { message = "File size must be under 10 MB." });

            // Soft-delete any existing document of same type for this student
            var existing = await _context.StudentDocuments
                .Where(d => d.StudentId == studentId && d.DocumentType == documentType && d.Active == true)
                .ToListAsync();
            foreach (var old in existing)
            {
                old.Active = false;
                old.LastModifiedBy = User.Identity?.Name ?? "system";
                old.LastModifiedDateTime = DateTime.Now;
            }

            // Build file path
            var uploadsRoot = Path.Combine(_env.ContentRootPath, "Uploads", "student-docs", studentId.ToString());
            Directory.CreateDirectory(uploadsRoot);

            var ext = Path.GetExtension(file.FileName);
            var safeFileName = $"{documentType}_{DateTime.Now:yyyyMMdd_HHmmss}{ext}";
            var fullPath = Path.Combine(uploadsRoot, safeFileName);

            using (var stream = System.IO.File.Create(fullPath))
                await file.CopyToAsync(stream);

            // Relative path stored in DB (for portability)
            var relPath = Path.Combine("student-docs", studentId.ToString(), safeFileName);

            var doc = new StudentDocument
            {
                StudentId     = studentId,
                DocumentType  = documentType,
                FileName      = file.FileName,
                FilePath      = relPath,
                ContentType   = file.ContentType,
                Active        = true,
                CreatedBy     = User.Identity?.Name ?? "system",
                CreatedDateTime = DateTime.Now
            };

            _context.StudentDocuments.Add(doc);

            // Also update the boolean flag on Student
            var student = await _context.Students.FindAsync(studentId);
            if (student != null)
            {
                if (documentType == "BirthCertificate") student.HasBirthCertificate = true;
                if (documentType == "NtmiMedical")      student.HasNtmiMedical = true;
                if (documentType == "NicCopy")          student.HasNicCopy = true;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Document uploaded successfully.",
                id = doc.Id,
                documentType,
                fileName = safeFileName,
                contentType = file.ContentType
            });
        }

        // ── GET /api/student-document/file/{id} ───────────────────────────────
        [HttpGet("file/{id:int}")]
        public async Task<IActionResult> GetFile(int id)
        {
            var doc = await _context.StudentDocuments
                .FirstOrDefaultAsync(d => d.Id == id && d.Active == true);

            if (doc == null)
                return NotFound(new { message = "Document not found." });

            var fullPath = Path.Combine(_env.ContentRootPath, "Uploads", doc.FilePath);
            if (!System.IO.File.Exists(fullPath))
                return NotFound(new { message = "File not found on disk." });

            var contentType = doc.ContentType ?? "application/octet-stream";
            var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
            return File(fileBytes, contentType, doc.FileName);
        }

        // ── DELETE /api/student-document/{id} ─────────────────────────────────
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Company Admin,Admin,Branch Admin,Staff")]
        public async Task<IActionResult> Delete(int id)
        {
            var doc = await _context.StudentDocuments.FindAsync(id);
            if (doc == null || doc.Active != true)
                return NotFound(new { message = "Document not found." });

            doc.Active = false;
            doc.LastModifiedBy = User.Identity?.Name ?? "system";
            doc.LastModifiedDateTime = DateTime.Now;

            // Check if any active docs remain for this type — if not, clear flag
            var student = await _context.Students.FindAsync(doc.StudentId);
            if (student != null)
            {
                await _context.SaveChangesAsync();
                var stillHas = await _context.StudentDocuments
                    .AnyAsync(d => d.StudentId == doc.StudentId && d.DocumentType == doc.DocumentType && d.Active == true);
                if (!stillHas)
                {
                    if (doc.DocumentType == "BirthCertificate") student.HasBirthCertificate = false;
                    if (doc.DocumentType == "NtmiMedical")      student.HasNtmiMedical = false;
                    if (doc.DocumentType == "NicCopy")          student.HasNicCopy = false;
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Document removed." });
        }
    }
}
