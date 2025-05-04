namespace Evidence_MasterDetails_SinglePage.Models
{
    public class Experience
    {
        public int ExperienceId { get; set; }
        public int? ApplicantId { get; set; }
        public string CompanyName { get; set; }
        public string Designation { get; set; }
        public int? YearsWorked { get; set; }

        public virtual Applicant Applicant { get; set; }

    }
}
