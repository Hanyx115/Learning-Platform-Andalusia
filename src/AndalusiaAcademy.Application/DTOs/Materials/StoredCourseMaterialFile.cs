namespace AndalusiaAcademy.Application.DTOs.Materials;

public sealed record StoredCourseMaterialFile(
    string StoredFileName,
    string OriginalFileName,
    string ContentType,
    long SizeBytes);
