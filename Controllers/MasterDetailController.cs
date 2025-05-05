using CrudWithSpSap.Data;
using CrudWithSpSap.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CrudWithSpSap.Controllers
{
    public class MasterDetailController : Controller
    {

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHost;
        private readonly IConfiguration _config;

        public MasterDetailController(ApplicationDbContext context, IWebHostEnvironment webHost, IConfiguration config)
        {
            _context = context;
            _webHost = webHost;
            _config = config;
        }

        private string GetConnectionString()
        {
            return _config.GetConnectionString("DefaultConnection");
        }

        public IActionResult Index()
        {
            List<Applicant> applicants = new List<Applicant>();
            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            {
                SqlCommand cmd = new SqlCommand("sp_GetAllApplicants", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    applicants.Add(new Applicant
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Name = reader["Name"].ToString(),
                        Gender = reader["Gender"].ToString(),
                        //DOB = Convert.ToDateTime(reader["DOB"]),
                        DOB = DateOnly.FromDateTime(Convert.ToDateTime(reader["DOB"])),
                        Age = reader["Age"] as int?,
                        Qualification = reader["Qualification"].ToString(),
                        TotalExperience = reader["TotalExperience"] as int?,
                        Date = Convert.ToDateTime(reader["Date"]),
                        PhotoUrl = reader["PhotoUrl"].ToString()
                    });
                }
            }
            return View(applicants);
        }

        [HttpGet]
        public IActionResult Create()
        {
            Applicant applicant = new Applicant();
            applicant.Experience.Add(new Experience());
            return View(applicant);
        }

        [HttpPost]
        public IActionResult Create(Applicant applicant)
        {
            if (applicant.DOB != default)
            {
                var today = DateOnly.FromDateTime(DateTime.Today);
                var age = today.Year - applicant.DOB.Year;
                if (applicant.DOB > today.AddYears(-age)) age--;
                applicant.Age = age;
            }

            applicant.Experience = applicant.Experience
                .Where(e => !string.IsNullOrEmpty(e.CompanyName) &&
                            !string.IsNullOrEmpty(e.Designation) &&
                            e.YearsWorked.HasValue)
                .ToList();

            applicant.PhotoUrl = GetUploadedFileName(applicant);

            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            {
                SqlCommand cmd = new SqlCommand("sp_CreateApplicantWithExperience", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@Date", applicant.Date);
                cmd.Parameters.AddWithValue("@Name", applicant.Name);
                cmd.Parameters.AddWithValue("@Gender", applicant.Gender);
                cmd.Parameters.AddWithValue("@DOB", applicant.DOB);
                cmd.Parameters.AddWithValue("@Age", (object)applicant.Age ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Qualification", applicant.Qualification);
                cmd.Parameters.AddWithValue("@TotalExperience", (object)applicant.TotalExperience ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PhotoUrl", applicant.PhotoUrl);

                // Create table-valued parameter for experience
                DataTable expTable = new DataTable();
                expTable.Columns.Add("CompanyName", typeof(string));
                expTable.Columns.Add("Designation", typeof(string));
                expTable.Columns.Add("YearsWorked", typeof(int));

                foreach (var exp in applicant.Experience)
                {
                    expTable.Rows.Add(exp.CompanyName, exp.Designation, exp.YearsWorked);
                }

                SqlParameter expParam = cmd.Parameters.AddWithValue("@ExperienceType", expTable);
                expParam.SqlDbType = SqlDbType.Structured;
                expParam.TypeName = "dbo.ExperienceType"; // Make sure this matches your SQL User-Defined Table Type

                conn.Open();
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Index");
        }

        private string GetUploadedFileName(Applicant applicant)
        {
            string uniqueFileName = null;
            if (applicant.ProfilePhoto != null)
            {
                string uploadsFolder = Path.Combine(_webHost.WebRootPath, "Images");
                uniqueFileName = Guid.NewGuid().ToString() + "_" + applicant.ProfilePhoto.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    applicant.ProfilePhoto.CopyTo(fileStream);
                }
            }
            return uniqueFileName;
        }

        public IActionResult Details(int id)
        {
            Applicant applicant = null;
            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            {
                SqlCommand cmd = new SqlCommand("sp_GetApplicantById", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Id", id);

                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    applicant = new Applicant
                    {
                        Id = id,
                        Name = reader["Name"].ToString(),
                        Gender = reader["Gender"].ToString(),
                        DOB = DateOnly.FromDateTime(Convert.ToDateTime(reader["DOB"])),
                        Age = reader["Age"] as int?,
                        Qualification = reader["Qualification"].ToString(),
                        TotalExperience = reader["TotalExperience"] as int?,
                        Date = Convert.ToDateTime(reader["Date"]),
                        PhotoUrl = reader["PhotoUrl"].ToString(),
                        Experience = new List<Experience>()
                    };
                }

                // এক্সপেরিয়েন্স ডাটা লোড করা
                if (reader.NextResult())
                {
                    while (reader.Read())
                    {
                        applicant.Experience.Add(new Experience
                        {
                            ExperienceId = Convert.ToInt32(reader["ExperienceId"]),
                            CompanyName = reader["CompanyName"].ToString(),
                            Designation = reader["Designation"].ToString(),
                            YearsWorked = Convert.ToInt32(reader["YearsWorked"])
                        });
                    }
                }
            }

            return PartialView("_Details", applicant);
        }
        public IActionResult EditPopup(int id)
        {
            try
            {
                var applicant = GetApplicantById(id);
                if (applicant == null)
                {
                    return NotFound($"Applicant with ID {id} not found.");
                }
                return PartialView("_EditApplicantPartial", applicant);
            }
            catch (Exception ex)
            {
                // Log the error
                return StatusCode(500, $"Error loading applicant data: {ex.Message}");
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditConfirmed([FromForm] Applicant applicant)
        {
            try
            {
                // Get existing applicant data first
                var existingApplicant = GetApplicantById(applicant.Id);
                if (existingApplicant == null)
                {
                    return NotFound(new { success = false, message = "Applicant not found." });
                }

                // Calculate age if DOB is provided
                if (applicant.DOB != default)
                {
                    var today = DateOnly.FromDateTime(DateTime.Today);
                    var age = today.Year - applicant.DOB.Year;
                    if (applicant.DOB > today.AddYears(-age)) age--;
                    applicant.Age = age;
                }

                // Validate experience
                applicant.Experience = applicant.Experience?
                    .Where(e => !string.IsNullOrEmpty(e.CompanyName) &&
                               !string.IsNullOrEmpty(e.Designation) &&
                               e.YearsWorked.HasValue)
                    .ToList() ?? new List<Experience>();

                // Handle photo upload - only update if new photo is provided
                if (applicant.ProfilePhoto != null && applicant.ProfilePhoto.Length > 0)
                {
                    // Delete old photo if exists
                    if (!string.IsNullOrEmpty(existingApplicant.PhotoUrl))
                    {
                        var oldFilePath = Path.Combine(_webHost.WebRootPath, "Images", existingApplicant.PhotoUrl);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }
                    // Save new photo
                    applicant.PhotoUrl = GetUploadedFileName(applicant);
                }
                else
                {
                    // Keep existing photo if no new photo is uploaded
                    applicant.PhotoUrl = existingApplicant.PhotoUrl;
                }

                // Rest of your update logic...
                using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            SqlCommand cmd = new SqlCommand("sp_UpdateApplicantWithExperience", conn, transaction);
                            cmd.CommandType = CommandType.StoredProcedure;

                            // Add all parameters including the preserved PhotoUrl
                            cmd.Parameters.AddWithValue("@Id", applicant.Id);
                            cmd.Parameters.AddWithValue("@Date", applicant.Date);
                            cmd.Parameters.AddWithValue("@Name", applicant.Name);
                            cmd.Parameters.AddWithValue("@Gender", applicant.Gender);
                            cmd.Parameters.AddWithValue("@DOB", applicant.DOB);
                            cmd.Parameters.AddWithValue("@Age", applicant.Age ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@Qualification", applicant.Qualification);
                            cmd.Parameters.AddWithValue("@TotalExperience", applicant.TotalExperience ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@PhotoUrl", applicant.PhotoUrl ?? string.Empty);

                            // Experience table parameter
                            DataTable expTable = new DataTable();
                            expTable.Columns.Add("CompanyName", typeof(string));
                            expTable.Columns.Add("Designation", typeof(string));
                            expTable.Columns.Add("YearsWorked", typeof(int));

                            foreach (var exp in applicant.Experience)
                            {
                                expTable.Rows.Add(exp.CompanyName, exp.Designation, exp.YearsWorked);
                            }

                            SqlParameter expParam = cmd.Parameters.AddWithValue("@ExperienceType", expTable);
                            expParam.SqlDbType = SqlDbType.Structured;
                            expParam.TypeName = "dbo.ExperienceType";

                            cmd.ExecuteNonQuery();
                            transaction.Commit();

                            return Json(new { success = true, message = "Update successful!" });
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            return StatusCode(500, new { success = false, message = ex.Message });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            {
                SqlCommand cmd = new SqlCommand("sp_DeleteApplicant", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Id", id);

                conn.Open();
                cmd.ExecuteNonQuery();
            }

            return RedirectToAction("Index");
        }

        private Applicant GetApplicantById(int id)
        {
            Applicant applicant = null;

            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            {
                SqlCommand cmd = new SqlCommand("sp_GetApplicantById", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Id", id);

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        applicant = new Applicant
                        {
                            Id = (int)reader["Id"],
                            Date = Convert.ToDateTime(reader["Date"]),
                            Name = reader["Name"].ToString(),
                            Gender = reader["Gender"].ToString(),
                            DOB = DateOnly.FromDateTime(Convert.ToDateTime(reader["DOB"])),
                            Age = reader["Age"] != DBNull.Value ? Convert.ToInt32(reader["Age"]) : (int?)null,
                            Qualification = reader["Qualification"].ToString(),
                            TotalExperience = reader["TotalExperience"] != DBNull.Value ? Convert.ToInt32(reader["TotalExperience"]) : (int?)null,
                            PhotoUrl = reader["PhotoUrl"]?.ToString(),
                            Experience = new List<Experience>()
                        };
                    }
                }

                if (applicant != null)
                {
                    // Get Experience
                    SqlCommand expCmd = new SqlCommand("sp_GetExperiencesByApplicantId", conn);
                    expCmd.CommandType = CommandType.StoredProcedure;
                    expCmd.Parameters.AddWithValue("@ApplicantId", id);

                    using (SqlDataReader expReader = expCmd.ExecuteReader())
                    {
                        while (expReader.Read())
                        {
                            applicant.Experience.Add(new Experience
                            {
                                ExperienceId = (int)expReader["ExperienceId"],
                                CompanyName = expReader["CompanyName"].ToString(),
                                Designation = expReader["Designation"].ToString(),
                                YearsWorked = expReader["YearsWorked"] != DBNull.Value ? Convert.ToInt32(expReader["YearsWorked"]) : (int?)null
                            });
                        }
                    }
                }
            }

            return applicant;
        }
        public IActionResult ApplicantStatus()
        {
            // Count
            int applicantCount = _context.Applicant.Count();
            ViewBag.Count = applicantCount;

            // Average Age
            double? avgAge = _context.Applicant.Average(a => (double?)a.Age);
            ViewBag.AverageAge = avgAge ?? 0;

            // Minimum Age + Name
            var minAgeApplicant = _context.Applicant
                .OrderBy(a => a.Age)
                .Select(a => new { a.Age, a.Name })
                .FirstOrDefault();

            ViewBag.MinAge = minAgeApplicant != null
                ? $"{minAgeApplicant.Age} ({minAgeApplicant.Name})"
                : "N/A";

            // Maximum Age + Name
            var maxAgeApplicant = _context.Applicant
                .OrderByDescending(a => a.Age)
                .Select(a => new { a.Age, a.Name })
                .FirstOrDefault();

            ViewBag.MaxAge = maxAgeApplicant != null
                ? $"{maxAgeApplicant.Age} ({maxAgeApplicant.Name})"
                : "N/A";

            // Sum of TotalExperience
            int? totalExp = _context.Applicant.Sum(a => (int?)a.TotalExperience);
            ViewBag.TotalExperience = totalExp ?? 0;

            // Max Experience + Name
            var maxExpApplicant = _context.Applicant
                .OrderByDescending(a => a.TotalExperience)
                .Select(a => new { a.TotalExperience, a.Name })
                .FirstOrDefault();

            ViewBag.MaxExperience = maxExpApplicant != null
                ? $"{maxExpApplicant.TotalExperience} ({maxExpApplicant.Name})"
                : "N/A";

            // Grouping (example: by Qualification)
            var groupedByQualification = _context.Applicant
                .GroupBy(a => a.Qualification)
                .Select(g => new
                {
                    Qualification = g.Key,
                    Count = g.Count()
                }).ToList();
            ViewBag.GroupedByQualification = groupedByQualification;

            return View();
        }
    }
}
