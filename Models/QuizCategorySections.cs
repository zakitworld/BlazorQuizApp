namespace BlazorQuizApp.Models;

public static class QuizCategorySections
{
    public const string TechAndCyber = "Tech & Cybersecurity";
    public const string NationalExams = "National Exams (WAEC)";
    public const string ProfessionalLicensure = "Teacher & Licensure Exams";
    public const string General = "General & Foundations";

    // Short convenience aliases
    public const string Tech = TechAndCyber;
    public const string Waec = NationalExams;
    public const string Licensure = ProfessionalLicensure;

    public static bool IsStandardizedExam(string? category)
    {
        var section = GetSection(category);
        return section == NationalExams || section == ProfessionalLicensure;
    }

    public static string GetSection(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return General;

        return category.Trim() switch
        {
            "Computing" or "ICT" or
            "Networking" or "Cybersecurity" or "Ethical Hacking" or "Penetration Testing" or
            "C#" or "ASP.NET Core" or "Blazor" or "JavaScript" or "Database" or
            "Entity Framework" or "Web Development" or "Programming Concepts" => TechAndCyber,

            "BECE" or "WASSCE" => NationalExams,

            "TEACHERS PROMOTION" or "NTC Licensure Exams" => ProfessionalLicensure,

            _ => General
        };
    }

    public static string GetIcon(string? category) => (category?.Trim()) switch
    {
        "C#" => "💻",
        "ASP.NET Core" => "🌐",
        "Blazor" => "🔥",
        "JavaScript" => "⚡",
        "Web Development" => "🕸️",
        "Database" => "🗄️",
        "Entity Framework" => "🗃️",
        "Programming Concepts" => "📚",
        "Computing" => "🖥️",
        "ICT" => "💡",
        "Networking" => "📡",
        "Cybersecurity" => "🛡️",
        "Ethical Hacking" => "🕵️",
        "Penetration Testing" => "🎯",
        "BECE" => "🎓",
        "WASSCE" => "📜",
        "TEACHERS PROMOTION" => "👨‍🏫",
        "NTC Licensure Exams" => "📑",
        "General" => "🧠",
        _ => "📝"
    };

    public static string GetSectionBadgeClass(string? categoryOrSection)
    {
        var section = categoryOrSection switch
        {
            TechAndCyber or NationalExams or ProfessionalLicensure or General => categoryOrSection,
            _ => GetSection(categoryOrSection)
        };

        return section switch
        {
            TechAndCyber => "bg-primary",
            NationalExams => "bg-success",
            ProfessionalLicensure => "bg-warning text-dark",
            _ => "bg-secondary"
        };
    }

    public static readonly (string Name, int SortOrder)[] DefaultCategories =
    [
        ("General", 10),
        ("ASP.NET Core", 20),
        ("Blazor", 30),
        ("C#", 40),
        ("Database", 50),
        ("Entity Framework", 60),
        ("JavaScript", 70),
        ("Programming Concepts", 80),
        ("Web Development", 90),
        ("Computing", 95),
        ("ICT", 98),
        // Tech & Cybersecurity additions
        ("Networking", 100),
        ("Cybersecurity", 110),
        ("Ethical Hacking", 120),
        ("Penetration Testing", 130),
        // National Exams (WAEC)
        ("BECE", 200),
        ("WASSCE", 210),
        // Professional & Teacher Licensure
        ("TEACHERS PROMOTION", 300),
        ("NTC Licensure Exams", 310)
    ];
}
