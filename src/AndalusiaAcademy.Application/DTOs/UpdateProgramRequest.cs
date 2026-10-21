namespace AndalusiaAcademy.Application.DTOs;

public sealed record UpdateProgramRequest(string Title, string Description, string Status, decimal? Price);
