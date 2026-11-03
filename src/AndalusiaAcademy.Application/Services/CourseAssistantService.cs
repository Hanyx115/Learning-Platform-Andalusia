using AndalusiaAcademy.Application.Assistant;
using AndalusiaAcademy.Application.DTOs.Assistant;
using AndalusiaAcademy.Application.DTOs.Materials;
using AndalusiaAcademy.Application.Repositories;

namespace AndalusiaAcademy.Application.Services;

public sealed class CourseAssistantService(
    ICourseMaterialRepository materialRepository,
    ICourseAssistantProvider provider)
{
    private const int MaximumSources = 5;

    public async Task<CourseAssistantOutcome> AskAsync(
        Guid enrollmentId,
        Guid learnerUserId,
        CourseAssistantRequest request,
        CancellationToken cancellationToken)
    {
        var materials = await materialRepository.GetForLearnerEnrollmentAsync(
            enrollmentId,
            learnerUserId,
            cancellationToken);

        if (materials is null)
        {
            return CourseAssistantOutcome.Failure(
                CourseAssistantFailureKind.NotFound,
                "Enrollment not found or learning access is unavailable.");
        }

        var language = NormalizeLanguage(request.Language);
        var sources = SelectRelevantSources(materials, request.Question);

        if (sources.Count == 0)
        {
            var emptyAnswer = language == "ar"
                ? "لا توجد مواد منشورة لهذه الدورة حتى الآن. أضف المواد أولًا ثم اسألني مرة أخرى."
                : "There are no published materials for this course yet. Add course materials, then ask me again.";

            return CourseAssistantOutcome.Success(
                new CourseAssistantResponseDto(emptyAnswer, [], IsDemoMode: false));
        }

        var history = (request.History ?? [])
            .TakeLast(6)
            .Select(message => new CourseAssistantMessageDto(
                message.Role.Trim().ToLowerInvariant(),
                message.Content.Trim()))
            .ToArray();

        var providerResult = await provider.AskAsync(
            new CourseAssistantProviderRequest(
                request.Question.Trim(),
                language,
                history,
                sources),
            cancellationToken);

        if (!providerResult.Succeeded || string.IsNullOrWhiteSpace(providerResult.Answer))
        {
            return CourseAssistantOutcome.Failure(
                CourseAssistantFailureKind.Unavailable,
                providerResult.Error ?? "Oufa Shobeir is temporarily unavailable.");
        }

        var citations = sources
            .Select(source => new CourseAssistantCitationDto(
                source.MaterialId,
                source.Title,
                source.ResourceType,
                source.ResourceUrl,
                source.OriginalFileName,
                source.MaterialType))
            .ToArray();

        return CourseAssistantOutcome.Success(
            new CourseAssistantResponseDto(
                providerResult.Answer.Trim(),
                citations,
                providerResult.IsDemoMode));
    }

    private static IReadOnlyList<CourseAssistantSource> SelectRelevantSources(
        IReadOnlyList<CourseMaterialDto> materials,
        string question)
    {
        var terms = question
            .Split([' ', '\t', '\r', '\n', '.', ',', ':', ';', '?', '!', '،', '؟'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(term => term.Length >= 3)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var ranked = materials
            .Select((material, index) => new
            {
                Material = material,
                Index = index,
                Score = terms.Sum(term =>
                    Contains(material.Title, term) ? 3 :
                    Contains(material.Description, term) ? 1 : 0),
            })
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Index)
            .Take(MaximumSources)
            .Select(item => new CourseAssistantSource(
                item.Material.Id,
                item.Material.Title,
                item.Material.Description,
                item.Material.ResourceType,
                item.Material.ResourceUrl,
                item.Material.OriginalFileName,
                item.Material.MaterialType))
            .ToArray();

        return ranked;
    }

    private static bool Contains(string? value, string term)
        => value?.Contains(term, StringComparison.OrdinalIgnoreCase) == true;

    private static string NormalizeLanguage(string? language)
        => language?.StartsWith("ar", StringComparison.OrdinalIgnoreCase) == true ? "ar" : "en";
}
