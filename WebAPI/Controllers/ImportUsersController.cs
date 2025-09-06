using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Data;
using ExcelDataReader;
using WebAPI.Models;
using WebAPI.DTOs;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/import")]
    [Authorize(Roles = "Admin")]
    [Authorize(Policy = "NotFirstLogin")]
    public class ImportUsersController : ControllerBase
    {
        private readonly Infoeduka2Context _db;
        private readonly IConfiguration _cfg;

        public ImportUsersController(Infoeduka2Context db, IConfiguration cfg)
        {
            _db = db;
            _cfg = cfg;
            // Required by ExcelDataReader to support legacy .xls encodings
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }

        /// <summary>
        /// Upload an Excel (.xls/.xlsx) with columns:
        /// 1: FirstName, 2: LastName, 3: RoleName (student/nastavnik/professor/admin),
        /// 5: Email, 6: GroupId (students must have it; admins must not).
        /// (Column 4 is intentionally unused per your spec.)
        /// </summary>
        [HttpPost("users")]
        [RequestSizeLimit(100_000_000)] // 100 MB, adjust as needed
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ImportUsers([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is required.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext is not ".xls" and not ".xlsx")
                return BadRequest("Only .xls or .xlsx are supported.");

            // Determine current admin (CreatedById for ImportBatch)
            int? createdById = null;
            var emailClaim = User?.Identity?.Name;
            if (!string.IsNullOrWhiteSpace(emailClaim))
                createdById = await _db.Users.Where(u => u.Email == emailClaim).Select(u => (int?)u.Id).FirstOrDefaultAsync();

            // Default password for newly inserted users (configure in appsettings)
            var defaultPassword = _cfg["Import:DefaultPassword"] ?? "ChangeMe123!";
            var defaultPasswordHash = WebAPI.Security.PasswordHasher.Hash(defaultPassword);

    


            // Build the TVP schema for dbo.udt_ImportedUser
            var tvp = new DataTable();
            tvp.Columns.Add("FirstName", typeof(string));
            tvp.Columns.Add("LastName", typeof(string));
            tvp.Columns.Add("RoleName", typeof(string));   // e.g. "student", "nastavnik", "professor", "admin"
            tvp.Columns.Add("Email", typeof(string));
            tvp.Columns.Add("GroupId", typeof(int)).AllowDBNull = true;      // nullable; use DBNull.Value when null
            tvp.Columns.Add("CourseId", typeof(int)).AllowDBNull = true;      // optional; use DBNull.Value

            // Read Excel
            using var stream = file.OpenReadStream();
            using var reader = ExcelReaderFactory.CreateReader(stream);
            var ds = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = false }
            });

            if (ds.Tables.Count == 0) return BadRequest("The Excel file has no worksheets.");
            var sheet = ds.Tables[0]; // first sheet

            ////
            if (sheet.Columns.Count < 6)
                return BadRequest("Expected 6 columns: FirstName, LastName, RoleName, (unused), Email, GroupId.");
            ////
            

            // Helper to normalize text cell
            string S(object? o) => (o?.ToString() ?? "").Trim();

            // Per spec: 1=FirstName, 2=LastName, 3=RoleName, (4 unused), 5=Email, 6=GroupId
            // We'll try to skip a header row if it looks like headers.
            int startRow = 1; //int startRow=0; Staro!!
            if (sheet.Rows.Count > 0)
            {
                var r0 = sheet.Rows[0];
                var first = S(r0[0]).ToLowerInvariant();
                var second = S(r0[1]).ToLowerInvariant();
                if (first.Contains("ime") || second.Contains("prez"))
                    startRow = 1;
            }

            int importedRows = 0;
            for (int i = startRow; i < sheet.Rows.Count; i++)
            {
                var r = sheet.Rows[i];
                // guard against short rows
                if (sheet.Columns.Count < 6) break;

                var firstName = S(r[0]);
                var lastName = S(r[1]);
                var roleName = S(r[2]).ToLowerInvariant(); // we normalize in SQL too, but trim here
                var email = S(r[4]);
                var groupText = S(r[5]);

                if (string.IsNullOrWhiteSpace(firstName) &&
                    string.IsNullOrWhiteSpace(lastName) &&
                    string.IsNullOrWhiteSpace(email))
                {
                    continue; // skip empty lines
                }

                // GroupId: treat non-positive/empty as NULL to avoid Admin=0 issues
                int? groupId = null;
                if (int.TryParse(groupText, out var gid) && gid > 0)
                    groupId = gid;

                // Add row to TVP (CourseId optional → null)
                tvp.Rows.Add(firstName, lastName, roleName, email, groupId ?? (object)DBNull.Value, DBNull.Value);
                importedRows++;
            }

            if (importedRows == 0)
                return BadRequest("No valid rows detected in the first worksheet.");

            // Call stored proc with TVP
            int batchId;
            using (var conn = new SqlConnection(_db.Database.GetConnectionString()))
            using (var cmd = new SqlCommand("dbo.sp_BulkUpsertUsers", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                var pRows = cmd.Parameters.AddWithValue("@Rows", tvp);
                pRows.SqlDbType = SqlDbType.Structured;
                pRows.TypeName = "dbo.udt_ImportedUser";

                cmd.Parameters.Add(new SqlParameter("@CreatedById", SqlDbType.Int) { Value = (object?)createdById ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@DefaultPasswordHash", SqlDbType.NVarChar, 500) { Value = defaultPasswordHash });
                cmd.Parameters.Add(new SqlParameter("@SourceFileName", SqlDbType.NVarChar, 260) { Value = file.FileName });

                var pOut = new SqlParameter("@BatchId", SqlDbType.Int) { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(pOut);

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
                batchId = (int)(pOut.Value ?? 0);
            }

            // Summarize results from ImportRow
            var rows = await _db.ImportRows
                .Where(x => x.ImportBatchId == batchId)
                .Select(x => new { x.RowNumber, x.IsSuccess, x.Error })
                .OrderBy(x => x.RowNumber)
                .ToListAsync();

            var result = new ImportResultDto
            {
                BatchId = batchId,
                TotalRows = rows.Count,
                SuccessCount = rows.Count(r => r.IsSuccess),
                ErrorCount = rows.Count(r => !r.IsSuccess),
                Errors = rows.Where(r => !r.IsSuccess)
                             .Select(r => new ImportResultDto.RowError { RowNumber = r.RowNumber, Error = r.Error ?? "" })
                             .ToList()
            };

            return Ok(result);
        }

        /// <summary>
        /// Read a saved batch report.
        /// </summary>
        [HttpGet("batches/{id:int}")]
        public async Task<IActionResult> GetBatch(int id)
        {
            var batch = await _db.ImportBatches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
            if (batch == null) return NotFound();

            var rows = await _db.ImportRows.AsNoTracking()
                .Where(r => r.ImportBatchId == id)
                .OrderBy(r => r.RowNumber)
                .Select(r => new { r.RowNumber, r.IsSuccess, r.Error, r.Data })
                .ToListAsync();

            return Ok(new
            {
                batch.Id,
                batch.SourceFileName,
                batch.CreatedAt,
                batch.CreatedById,
                total = rows.Count,
                success = rows.Count(r => r.IsSuccess),
                errors = rows.Where(r => !r.IsSuccess).Select(r => new { r.RowNumber, r.Error }),
                sample = rows.Take(5)
            });
        }
    }
}
