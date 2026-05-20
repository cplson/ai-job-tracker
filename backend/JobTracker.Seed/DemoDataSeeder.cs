using JobTracker.Core.Entities;
using JobTracker.Core.Enums;
using JobTracker.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace JobTracker.Seed;

public static class DemoDataSeeder
{
    public const string DemoEmail = "demo@jobtracker.app";
    public const string DemoPassword = "Demo123!";

    public static async Task<int> RunAsync(AppDbContext db, bool force, CancellationToken cancellationToken = default)
    {
        await db.Database.EnsureCreatedAsync(cancellationToken);
        await DatabaseSchemaPatcher.ApplyAsync(db, NullLogger.Instance, cancellationToken);

        var existing = await db.Users
            .Include(u => u.Applications)
            .FirstOrDefaultAsync(u => u.Email == DemoEmail, cancellationToken);

        if (existing != null && existing.Applications.Count > 0 && !force)
        {
            Console.WriteLine($"Demo user already seeded ({DemoEmail}). Use --force to replace.");
            PrintCredentials();
            return 0;
        }

        if (existing != null)
        {
            await RemoveDemoUserDataAsync(db, existing.Id, cancellationToken);
            db.Users.Remove(existing);
            await db.SaveChangesAsync(cancellationToken);
            Console.WriteLine("Removed existing demo data.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = DemoEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(DemoPassword),
        };
        db.Users.Add(user);

        var resumeSoftware = new Resume
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Name = "Software Engineer Resume.pdf",
            FilePath = "uploads/demo-software-engineer.pdf",
            ExtractedText =
                "Jane Demo — Software Engineer with 4 years building web APIs in C# and React. " +
                "Experience with PostgreSQL, Docker, and CI/CD.",
            UploadedAt = DateTime.UtcNow.AddDays(-30),
        };

        var resumeFullStack = new Resume
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Name = "Full Stack Resume.pdf",
            FilePath = "uploads/demo-full-stack.pdf",
            ExtractedText =
                "Jane Demo — Full stack developer skilled in TypeScript, .NET, and cloud deployment. " +
                "Led capstone projects and internship deliverables.",
            UploadedAt = DateTime.UtcNow.AddDays(-20),
        };

        db.Resumes.AddRange(resumeSoftware, resumeFullStack);

        var applications = BuildApplications(user.Id, resumeSoftware.Id, resumeFullStack.Id);
        db.Applications.AddRange(applications);

        var aiJobs = BuildAiJobs(applications);
        db.AIJobs.AddRange(aiJobs);

        await db.SaveChangesAsync(cancellationToken);

        Console.WriteLine($"Seeded demo user with {applications.Count} applications and {aiJobs.Count} AI analyses.");
        PrintCredentials();
        return 0;
    }

    private static void PrintCredentials()
    {
        Console.WriteLine();
        Console.WriteLine("  Login:    " + DemoEmail);
        Console.WriteLine("  Password: " + DemoPassword);
        Console.WriteLine();
    }

    private static async Task RemoveDemoUserDataAsync(
        AppDbContext db,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var applicationIds = await db.Applications
            .Where(a => a.UserId == userId)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);

        if (applicationIds.Count > 0)
        {
            var aiJobs = await db.AIJobs
                .Where(j => applicationIds.Contains(j.ApplicationId))
                .ToListAsync(cancellationToken);
            db.AIJobs.RemoveRange(aiJobs);
        }

        var applications = await db.Applications
            .Where(a => a.UserId == userId)
            .ToListAsync(cancellationToken);
        db.Applications.RemoveRange(applications);

        var resumes = await db.Resumes
            .Where(r => r.UserId == userId)
            .ToListAsync(cancellationToken);
        db.Resumes.RemoveRange(resumes);
    }

    private static List<Application> BuildApplications(Guid userId, Guid resumeSoftwareId, Guid resumeFullStackId)
    {
        var specs = new (string Company, string JobTitle, string Description, ApplicationStatus Status, int DaysAgo, Guid? ResumeId)[]
        {
            ("Northwind Labs", "Junior Software Engineer", "Build internal tools with C# and React.", ApplicationStatus.Applied, 2, resumeSoftwareId),
            ("Contoso Health", "Backend Developer", "API development with .NET and PostgreSQL.", ApplicationStatus.Interviewing, 5, resumeSoftwareId),
            ("Fabrikam Digital", "Full Stack Developer", "End-to-end features for customer portal.", ApplicationStatus.Offer, 8, resumeFullStackId),
            ("Adventure Works", "Software Engineer II", "Microservices and Docker deployments.", ApplicationStatus.Rejected, 12, resumeSoftwareId),
            ("Tailspin Toys", "Web Developer Intern", "Summer internship — front-end focus.", ApplicationStatus.Draft, 1, null),
            ("Wide World Importers", ".NET Developer", "Maintain legacy apps; modernize gradually.", ApplicationStatus.Applied, 18, resumeSoftwareId),
            ("Blue Yonder Airlines", "API Engineer", "Design REST APIs for booking systems.", ApplicationStatus.Interviewing, 22, resumeFullStackId),
            ("Litware Inc", "DevOps-minded Developer", "CI/CD pipelines and cloud hosting.", ApplicationStatus.Applied, 25, resumeFullStackId),
            ("Proseware", "Graduate Developer", "Rotational program across teams.", ApplicationStatus.Draft, 3, null),
            ("Margie's Travel", "React Developer", "Customer-facing booking UI.", ApplicationStatus.Rejected, 30, resumeFullStackId),
            ("Fourth Coffee", "Software Engineer", "POS integrations and reporting.", ApplicationStatus.Applied, 14, resumeSoftwareId),
            ("Graphic Design Institute", "Teaching Assistant (Tech)", "Support web programming courses.", ApplicationStatus.Offer, 40, resumeFullStackId),
            ("Humongous Insurance", "Application Developer", "Internal policy management system.", ApplicationStatus.Interviewing, 10, resumeSoftwareId),
            ("Spike Video Store", "Part-time Web Developer", "Evening shifts; portfolio site maintenance.", ApplicationStatus.Draft, 6, null),
            ("City Power & Light", "Energy Data Engineer", "Data pipelines — stretch role.", ApplicationStatus.Applied, 35, resumeSoftwareId),
        };

        return specs.Select(s => new Application
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Company = s.Company,
            JobTitle = s.JobTitle,
            JobDescription = s.Description,
            Status = s.Status,
            CreatedAt = DateTime.UtcNow.AddDays(-s.DaysAgo),
            ResumeId = s.ResumeId,
        }).ToList();
    }

    private static List<AIJob> BuildAiJobs(List<Application> applications)
    {
        var completed = applications
            .Where(a => a.ResumeId.HasValue && a.Status is ApplicationStatus.Applied or ApplicationStatus.Interviewing or ApplicationStatus.Offer)
            .Take(4)
            .ToList();

        return completed.Select((app, i) => new AIJob
        {
            Id = Guid.NewGuid(),
            ApplicationId = app.Id,
            JobType = "ResumeAnalysis",
            Status = AIJobStatus.Completed,
            CreatedAt = app.CreatedAt.AddHours(2),
            Result =
                "{\"summary\":\"Strong alignment with role requirements.\"," +
                "\"strengths\":[\"Relevant stack experience\",\"Clear project examples\"]," +
                "\"weaknesses\":[\"Could highlight metrics more\"]," +
                "\"suggestions\":[\"Tailor summary to " + app.Company + "\"]," +
                "\"matchScore\":" + (72 + i * 5) + "}",
        }).ToList();
    }
}
