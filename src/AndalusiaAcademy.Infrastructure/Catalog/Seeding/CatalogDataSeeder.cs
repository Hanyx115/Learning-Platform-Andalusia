using AndalusiaAcademy.Domain.Models;
using AndalusiaAcademy.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AndalusiaAcademy.Infrastructure.Catalog.Seeding;

public static class CatalogDataSeeder
{
    public static async Task<int> SeedAsync(
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        var addedCount = 0;

        var categoriesBySlug = (await dbContext.CourseCategories
                .AsNoTracking()
                .ToListAsync(cancellationToken))
            .ToDictionary(category => category.Slug, StringComparer.OrdinalIgnoreCase);

        foreach (var seed in Categories)
        {
            if (categoriesBySlug.ContainsKey(seed.Slug))
            {
                continue;
            }

            var category = new CourseCategory
            {
                Id = seed.Id,
                Name = seed.Name,
                Slug = seed.Slug,
            };

            dbContext.CourseCategories.Add(category);
            categoriesBySlug.Add(category.Slug, category);
            addedCount++;
        }

        var coursesBySlug = (await dbContext.Courses
                .AsNoTracking()
                .ToListAsync(cancellationToken))
            .ToDictionary(course => course.Slug, StringComparer.OrdinalIgnoreCase);

        foreach (var seed in Courses)
        {
            if (coursesBySlug.ContainsKey(seed.Slug))
            {
                continue;
            }

            var course = new Course
            {
                Id = seed.Id,
                CategoryId = categoriesBySlug[seed.CategorySlug].Id,
                Title = seed.Title,
                Slug = seed.Slug,
                Description = seed.Description,
                Status = "Published",
                Type = seed.Type,
                CreatedAt = seed.CreatedAt,
            };

            dbContext.Courses.Add(course);
            coursesBySlug.Add(course.Slug, course);
            addedCount++;
        }

        var programsBySlug = (await dbContext.Programs
                .AsNoTracking()
                .ToListAsync(cancellationToken))
            .ToDictionary(program => program.Slug, StringComparer.OrdinalIgnoreCase);

        foreach (var seed in Programs)
        {
            if (programsBySlug.ContainsKey(seed.Slug))
            {
                continue;
            }

            var program = new AcademyProgram
            {
                Id = seed.Id,
                Title = seed.Title,
                Slug = seed.Slug,
                Description = seed.Description,
                Status = "Published",
                CreatedAt = seed.CreatedAt,
            };

            dbContext.Programs.Add(program);
            programsBySlug.Add(program.Slug, program);
            addedCount++;
        }

        var programCourseKeys = (await dbContext.ProgramCourses
                .AsNoTracking()
                .Select(link => new { link.ProgramId, link.CourseId })
                .ToListAsync(cancellationToken))
            .Select(link => (link.ProgramId, link.CourseId))
            .ToHashSet();

        foreach (var seed in ProgramCourses)
        {
            var programId = programsBySlug[seed.ProgramSlug].Id;
            var courseId = coursesBySlug[seed.CourseSlug].Id;

            if (!programCourseKeys.Add((programId, courseId)))
            {
                continue;
            }

            dbContext.ProgramCourses.Add(new ProgramCourse
            {
                ProgramId = programId,
                CourseId = courseId,
                SequenceOrder = seed.SequenceOrder,
            });
            addedCount++;
        }

        var careerPathsBySlug = (await dbContext.CareerPaths
                .AsNoTracking()
                .ToListAsync(cancellationToken))
            .ToDictionary(careerPath => careerPath.Slug, StringComparer.OrdinalIgnoreCase);

        foreach (var seed in CareerPaths)
        {
            if (careerPathsBySlug.ContainsKey(seed.Slug))
            {
                continue;
            }

            var careerPath = new CareerPath
            {
                Id = seed.Id,
                Title = seed.Title,
                Slug = seed.Slug,
                Description = seed.Description,
                RecommendedSkills = seed.RecommendedSkills,
                Status = "Published",
            };

            dbContext.CareerPaths.Add(careerPath);
            careerPathsBySlug.Add(careerPath.Slug, careerPath);
            addedCount++;
        }

        var careerPathProgramKeys = (await dbContext.CareerPathPrograms
                .AsNoTracking()
                .Select(link => new { link.CareerPathId, link.ProgramId })
                .ToListAsync(cancellationToken))
            .Select(link => (link.CareerPathId, link.ProgramId))
            .ToHashSet();

        foreach (var seed in CareerPathPrograms)
        {
            var careerPathId = careerPathsBySlug[seed.CareerPathSlug].Id;
            var programId = programsBySlug[seed.ProgramSlug].Id;

            if (!careerPathProgramKeys.Add((careerPathId, programId)))
            {
                continue;
            }

            dbContext.CareerPathPrograms.Add(new CareerPathProgram
            {
                CareerPathId = careerPathId,
                ProgramId = programId,
            });
            addedCount++;
        }

        var careerPathCourseKeys = (await dbContext.CareerPathCourses
                .AsNoTracking()
                .Select(link => new { link.CareerPathId, link.CourseId })
                .ToListAsync(cancellationToken))
            .Select(link => (link.CareerPathId, link.CourseId))
            .ToHashSet();

        foreach (var seed in CareerPathCourses)
        {
            var careerPathId = careerPathsBySlug[seed.CareerPathSlug].Id;
            var courseId = coursesBySlug[seed.CourseSlug].Id;

            if (!careerPathCourseKeys.Add((careerPathId, courseId)))
            {
                continue;
            }

            dbContext.CareerPathCourses.Add(new CareerPathCourse
            {
                CareerPathId = careerPathId,
                CourseId = courseId,
            });
            addedCount++;
        }

        if (addedCount > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return addedCount;
    }

    private static readonly CategorySeed[] Categories =
    [
        new(new Guid("10000000-0000-0000-0000-000000000001"), "Clinical Care", "clinical-care"),
        new(new Guid("10000000-0000-0000-0000-000000000002"), "Emergency Medicine", "emergency-medicine"),
        new(new Guid("10000000-0000-0000-0000-000000000003"), "Patient Safety", "patient-safety"),
        new(new Guid("10000000-0000-0000-0000-000000000004"), "Healthcare Leadership", "healthcare-leadership"),
        new(new Guid("10000000-0000-0000-0000-000000000005"), "Software Development", "software-development"),
        new(new Guid("10000000-0000-0000-0000-000000000006"), "Computer Systems", "computer-systems"),
    ];

    private static readonly CourseSeed[] Courses =
    [
        new(
            new Guid("20000000-0000-0000-0000-000000000001"),
            "emergency-medicine",
            "Basic Life Support Essentials",
            "basic-life-support-essentials",
            "Build a practical foundation in recognizing emergencies, delivering immediate support and working effectively within a response team.",
            "Offline",
            new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero)),
        new(
            new Guid("20000000-0000-0000-0000-000000000002"),
            "emergency-medicine",
            "Emergency Response Fundamentals",
            "emergency-response-fundamentals",
            "Develop structured assessment, escalation and teamwork skills for common urgent-care situations.",
            "Hybrid",
            new DateTimeOffset(2026, 1, 20, 0, 0, 0, TimeSpan.Zero)),
        new(
            new Guid("20000000-0000-0000-0000-000000000003"),
            "patient-safety",
            "Infection Prevention and Control",
            "infection-prevention-and-control",
            "Apply evidence-based precautions that reduce infection risk for patients, visitors and healthcare teams.",
            "Online",
            new DateTimeOffset(2026, 2, 5, 0, 0, 0, TimeSpan.Zero)),
        new(
            new Guid("20000000-0000-0000-0000-000000000004"),
            "patient-safety",
            "Patient Safety Essentials",
            "patient-safety-essentials",
            "Understand safety culture, incident prevention and practical approaches to improving patient outcomes.",
            "Online",
            new DateTimeOffset(2026, 2, 15, 0, 0, 0, TimeSpan.Zero)),
        new(
            new Guid("20000000-0000-0000-0000-000000000005"),
            "clinical-care",
            "Clinical Communication Skills",
            "clinical-communication-skills",
            "Strengthen patient-centered communication, handovers and collaboration across multidisciplinary teams.",
            "Hybrid",
            new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero)),
        new(
            new Guid("20000000-0000-0000-0000-000000000006"),
            "clinical-care",
            "Fundamentals of Patient Assessment",
            "fundamentals-of-patient-assessment",
            "Practice systematic observation, history taking and documentation for safe clinical decision support.",
            "Offline",
            new DateTimeOffset(2026, 3, 12, 0, 0, 0, TimeSpan.Zero)),
        new(
            new Guid("20000000-0000-0000-0000-000000000007"),
            "healthcare-leadership",
            "Healthcare Leadership Foundations",
            "healthcare-leadership-foundations",
            "Explore practical leadership, team motivation and accountable decision-making in healthcare settings.",
            "Online",
            new DateTimeOffset(2026, 4, 2, 0, 0, 0, TimeSpan.Zero)),
        new(
            new Guid("20000000-0000-0000-0000-000000000008"),
            "healthcare-leadership",
            "Quality Improvement in Healthcare",
            "quality-improvement-in-healthcare",
            "Use measurable improvement methods to identify service gaps and support safer, more reliable care.",
            "Hybrid",
            new DateTimeOffset(2026, 4, 18, 0, 0, 0, TimeSpan.Zero)),
        new(
            new Guid("20000000-0000-0000-0000-000000000009"),
            "software-development",
            "Programming Fundamentals",
            "programming-fundamentals",
            "Learn problem solving, variables, control flow, functions and the foundations of writing maintainable programs.",
            "Online",
            new DateTimeOffset(2026, 5, 2, 0, 0, 0, TimeSpan.Zero)),
        new(
            new Guid("20000000-0000-0000-0000-000000000010"),
            "software-development",
            "Object-Oriented Programming with C#",
            "object-oriented-programming-csharp",
            "Build modular applications using classes, interfaces, inheritance, composition and core C# practices.",
            "Hybrid",
            new DateTimeOffset(2026, 5, 12, 0, 0, 0, TimeSpan.Zero)),
        new(
            new Guid("20000000-0000-0000-0000-000000000011"),
            "software-development",
            "Data Structures and Algorithms",
            "data-structures-and-algorithms",
            "Understand common data structures, algorithmic problem solving and the performance tradeoffs behind software design decisions.",
            "Online",
            new DateTimeOffset(2026, 5, 22, 0, 0, 0, TimeSpan.Zero)),
        new(
            new Guid("20000000-0000-0000-0000-000000000012"),
            "computer-systems",
            "Computer Networks Fundamentals",
            "computer-networks-fundamentals",
            "Explore network models, addressing, routing, switching and the practical foundations of connected computer systems.",
            "Hybrid",
            new DateTimeOffset(2026, 6, 2, 0, 0, 0, TimeSpan.Zero)),
        new(
            new Guid("20000000-0000-0000-0000-000000000013"),
            "computer-systems",
            "Embedded Systems Essentials",
            "embedded-systems-essentials",
            "Connect software with hardware through microcontrollers, digital interfaces and foundational embedded-system design.",
            "Offline",
            new DateTimeOffset(2026, 6, 12, 0, 0, 0, TimeSpan.Zero)),
        new(
            new Guid("20000000-0000-0000-0000-000000000014"),
            "computer-systems",
            "Cybersecurity Foundations",
            "cybersecurity-foundations",
            "Develop an introductory understanding of threats, secure system design, access control and network protection.",
            "Online",
            new DateTimeOffset(2026, 6, 22, 0, 0, 0, TimeSpan.Zero)),
    ];

    private static readonly ProgramSeed[] Programs =
    [
        new(
            new Guid("30000000-0000-0000-0000-000000000001"),
            "Emergency Care Foundations",
            "emergency-care-foundations",
            "A structured pathway through essential assessment, immediate support and emergency teamwork skills.",
            new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero)),
        new(
            new Guid("30000000-0000-0000-0000-000000000002"),
            "Patient Safety and Quality",
            "patient-safety-and-quality",
            "A practical program covering infection prevention, safety culture and continuous quality improvement.",
            new DateTimeOffset(2026, 5, 8, 0, 0, 0, TimeSpan.Zero)),
        new(
            new Guid("30000000-0000-0000-0000-000000000003"),
            "Clinical Leadership Development",
            "clinical-leadership-development",
            "Develop communication, leadership and improvement skills for effective healthcare teams.",
            new DateTimeOffset(2026, 5, 15, 0, 0, 0, TimeSpan.Zero)),
        new(
            new Guid("30000000-0000-0000-0000-000000000004"),
            "Software Engineering Foundations",
            "software-engineering-foundations",
            "A progressive program covering programming, object-oriented design, data structures and algorithmic thinking.",
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero)),
        new(
            new Guid("30000000-0000-0000-0000-000000000005"),
            "Computer Systems Engineering",
            "computer-systems-engineering",
            "Build foundational capability across networks, embedded systems and secure computer infrastructure.",
            new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero)),
    ];

    private static readonly ProgramCourseSeed[] ProgramCourses =
    [
        new("emergency-care-foundations", "basic-life-support-essentials", 1),
        new("emergency-care-foundations", "fundamentals-of-patient-assessment", 2),
        new("emergency-care-foundations", "emergency-response-fundamentals", 3),
        new("patient-safety-and-quality", "infection-prevention-and-control", 1),
        new("patient-safety-and-quality", "patient-safety-essentials", 2),
        new("patient-safety-and-quality", "quality-improvement-in-healthcare", 3),
        new("clinical-leadership-development", "clinical-communication-skills", 1),
        new("clinical-leadership-development", "healthcare-leadership-foundations", 2),
        new("clinical-leadership-development", "quality-improvement-in-healthcare", 3),
        new("software-engineering-foundations", "programming-fundamentals", 1),
        new("software-engineering-foundations", "object-oriented-programming-csharp", 2),
        new("software-engineering-foundations", "data-structures-and-algorithms", 3),
        new("computer-systems-engineering", "computer-networks-fundamentals", 1),
        new("computer-systems-engineering", "embedded-systems-essentials", 2),
        new("computer-systems-engineering", "cybersecurity-foundations", 3),
    ];

    private static readonly CareerPathSeed[] CareerPaths =
    [
        new(
            new Guid("40000000-0000-0000-0000-000000000001"),
            "Emergency Care Practitioner",
            "emergency-care-practitioner",
            "Build the essential clinical and teamwork capabilities needed to contribute confidently in urgent-care environments.",
            "Rapid assessment, immediate support, emergency communication, multidisciplinary teamwork"),
        new(
            new Guid("40000000-0000-0000-0000-000000000002"),
            "Healthcare Quality Specialist",
            "healthcare-quality-specialist",
            "Prepare to support safer systems through infection prevention, incident awareness and measurable service improvement.",
            "Patient safety, infection prevention, quality measurement, process improvement"),
        new(
            new Guid("40000000-0000-0000-0000-000000000003"),
            "Clinical Team Leader",
            "clinical-team-leader",
            "Develop the communication and leadership foundation required to guide collaborative healthcare teams.",
            "Clinical communication, team leadership, decision-making, quality improvement"),
        new(
            new Guid("40000000-0000-0000-0000-000000000004"),
            "Software Engineer",
            "software-engineer",
            "Build the programming, design and problem-solving foundation needed to create reliable software systems.",
            "Programming, object-oriented design, algorithms, debugging, software design"),
        new(
            new Guid("40000000-0000-0000-0000-000000000005"),
            "Network and Infrastructure Engineer",
            "network-infrastructure-engineer",
            "Prepare to support connected systems through networking, infrastructure security and systematic troubleshooting.",
            "Networking, routing, systems security, troubleshooting, infrastructure operations"),
        new(
            new Guid("40000000-0000-0000-0000-000000000006"),
            "Embedded Systems Engineer",
            "embedded-systems-engineer",
            "Develop the hardware-software integration skills needed to design and troubleshoot embedded computing solutions.",
            "Microcontrollers, embedded programming, digital interfaces, debugging, computer architecture"),
    ];

    private static readonly CareerPathProgramSeed[] CareerPathPrograms =
    [
        new("emergency-care-practitioner", "emergency-care-foundations"),
        new("healthcare-quality-specialist", "patient-safety-and-quality"),
        new("clinical-team-leader", "clinical-leadership-development"),
        new("software-engineer", "software-engineering-foundations"),
        new("network-infrastructure-engineer", "computer-systems-engineering"),
        new("embedded-systems-engineer", "computer-systems-engineering"),
    ];

    private static readonly CareerPathCourseSeed[] CareerPathCourses =
    [
        new("emergency-care-practitioner", "basic-life-support-essentials"),
        new("emergency-care-practitioner", "emergency-response-fundamentals"),
        new("healthcare-quality-specialist", "infection-prevention-and-control"),
        new("healthcare-quality-specialist", "patient-safety-essentials"),
        new("healthcare-quality-specialist", "quality-improvement-in-healthcare"),
        new("clinical-team-leader", "clinical-communication-skills"),
        new("clinical-team-leader", "healthcare-leadership-foundations"),
        new("software-engineer", "programming-fundamentals"),
        new("software-engineer", "object-oriented-programming-csharp"),
        new("software-engineer", "data-structures-and-algorithms"),
        new("network-infrastructure-engineer", "computer-networks-fundamentals"),
        new("network-infrastructure-engineer", "cybersecurity-foundations"),
        new("embedded-systems-engineer", "embedded-systems-essentials"),
        new("embedded-systems-engineer", "computer-networks-fundamentals"),
    ];

    private sealed record CategorySeed(Guid Id, string Name, string Slug);

    private sealed record CourseSeed(
        Guid Id,
        string CategorySlug,
        string Title,
        string Slug,
        string Description,
        string Type,
        DateTimeOffset CreatedAt);

    private sealed record ProgramSeed(
        Guid Id,
        string Title,
        string Slug,
        string Description,
        DateTimeOffset CreatedAt);

    private sealed record ProgramCourseSeed(string ProgramSlug, string CourseSlug, int SequenceOrder);

    private sealed record CareerPathSeed(
        Guid Id,
        string Title,
        string Slug,
        string Description,
        string RecommendedSkills);

    private sealed record CareerPathProgramSeed(string CareerPathSlug, string ProgramSlug);

    private sealed record CareerPathCourseSeed(string CareerPathSlug, string CourseSlug);
}
