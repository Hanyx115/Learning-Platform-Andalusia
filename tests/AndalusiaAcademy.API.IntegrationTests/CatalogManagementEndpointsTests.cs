using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AndalusiaAcademy.Application.Identity;
using AndalusiaAcademy.Infrastructure.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace AndalusiaAcademy.API.IntegrationTests;

public sealed class CatalogManagementEndpointsTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Admin_CanCreateCategoryCourseAndProgram()
    {
        var adminToken = await RegisterAsAsync(RoleNames.Admin);
        var suffix = Guid.NewGuid().ToString("N");

        var categoryId = await CreateCategoryAsync($"Catalog Mgmt Category {suffix}", adminToken);

        using var courseResponse = await SendAuthorizedAsync(
            HttpMethod.Post, "/api/v1/courses", adminToken,
            new
            {
                title = $"Catalog Mgmt Course {suffix}",
                categoryId,
                description = "A course created through the admin management API.",
                status = "Published",
                type = "Online",
                price = 250.00m,
            });
        courseResponse.EnsureSuccessStatusCode();
        using var courseDocument = JsonDocument.Parse(await courseResponse.Content.ReadAsStringAsync());
        Assert.Equal("Published", courseDocument.RootElement.GetProperty("status").GetString());

        using var programResponse = await SendAuthorizedAsync(
            HttpMethod.Post, "/api/v1/programs", adminToken,
            new
            {
                title = $"Catalog Mgmt Program {suffix}",
                description = "A program created through the admin management API.",
                status = "Published",
                price = 500.00m,
            });
        programResponse.EnsureSuccessStatusCode();
        using var programDocument = JsonDocument.Parse(await programResponse.Content.ReadAsStringAsync());
        Assert.Equal("Published", programDocument.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Instructor_CannotCreateCourse()
    {
        var instructorToken = await RegisterAsAsync(RoleNames.Instructor);
        var adminToken = await RegisterAsAsync(RoleNames.Admin);
        var categoryId = await CreateCategoryAsync($"Forbidden Test Category {Guid.NewGuid():N}", adminToken);

        using var response = await SendAuthorizedAsync(
            HttpMethod.Post, "/api/v1/courses", instructorToken,
            new
            {
                title = "Should not be created",
                categoryId,
                description = "desc",
                status = "Published",
                type = "Online",
                price = (decimal?)null,
            });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ContentManager_CanEditCatalogContent_WithoutChangingPrice()
    {
        var adminToken = await RegisterAsAsync(RoleNames.Admin);
        var contentManagerToken = await RegisterAsAsync(RoleNames.ContentManager);
        var suffix = Guid.NewGuid().ToString("N");
        var categoryId = await CreateCategoryAsync($"Editorial Category {suffix}", contentManagerToken);

        using var createResponse = await SendAuthorizedAsync(
            HttpMethod.Post, "/api/v1/courses", adminToken,
            new
            {
                title = $"Editorial Course {suffix}",
                categoryId,
                description = "Original description.",
                status = "Draft",
                type = "Online",
                price = 925m,
            });
        createResponse.EnsureSuccessStatusCode();
        using var createDocument = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var courseId = createDocument.RootElement.GetProperty("id").GetGuid();

        using var updateResponse = await SendAuthorizedAsync(
            HttpMethod.Put, $"/api/v1/courses/{courseId}/content", contentManagerToken,
            new
            {
                title = $"Editorial Course Updated {suffix}",
                categoryId,
                description = "Updated by the content manager.",
                status = "Published",
                type = "Hybrid",
            });
        updateResponse.EnsureSuccessStatusCode();
        using var updateDocument = JsonDocument.Parse(await updateResponse.Content.ReadAsStringAsync());
        Assert.Equal(925m, updateDocument.RootElement.GetProperty("price").GetDecimal());
        Assert.Equal("Published", updateDocument.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task ContentManager_CannotUseOperationalCatalogEndpoints()
    {
        var contentManagerToken = await RegisterAsAsync(RoleNames.ContentManager);

        using var priceResponse = await SendAuthorizedAsync(
            HttpMethod.Post, "/api/v1/courses", contentManagerToken,
            new
            {
                title = "Forbidden priced course",
                categoryId = Guid.NewGuid(),
                description = "Not allowed.",
                status = "Published",
                type = "Online",
                price = 1m,
            });
        Assert.Equal(HttpStatusCode.Forbidden, priceResponse.StatusCode);

        using var offeringResponse = await SendAuthorizedAsync(
            HttpMethod.Post, $"/api/v1/courses/{Guid.NewGuid()}/offerings", contentManagerToken,
            new
            {
                startsAtUtc = DateTimeOffset.UtcNow.AddDays(2),
                endsAtUtc = DateTimeOffset.UtcNow.AddDays(3),
                applicationOpensAtUtc = DateTimeOffset.UtcNow,
                applicationClosesAtUtc = DateTimeOffset.UtcNow.AddDays(1),
                price = 1m,
                currency = "EGP",
                capacity = 10,
                status = "Open",
            });
        Assert.Equal(HttpStatusCode.Forbidden, offeringResponse.StatusCode);
    }

    [Fact]
    public async Task ContentManager_CanCreateUpdateAndDeleteCareerPath_WhileLearnerCannot()
    {
        var contentManagerToken = await RegisterAsAsync(RoleNames.ContentManager);
        var learnerToken = await RegisterAsAsync(RoleNames.Learner);
        var suffix = Guid.NewGuid().ToString("N");
        var request = new
        {
            title = $"Security Career {suffix}",
            description = "A managed career path.",
            recommendedSkills = "Security, networking",
            status = "Draft",
            recommendedProgramIds = Array.Empty<Guid>(),
            recommendedCourseIds = Array.Empty<Guid>(),
        };

        using var learnerResponse = await SendAuthorizedAsync(
            HttpMethod.Post, "/api/v1/career-paths", learnerToken, request);
        Assert.Equal(HttpStatusCode.Forbidden, learnerResponse.StatusCode);

        using var createResponse = await SendAuthorizedAsync(
            HttpMethod.Post, "/api/v1/career-paths", contentManagerToken, request);
        createResponse.EnsureSuccessStatusCode();
        using var createDocument = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var careerPathId = createDocument.RootElement.GetProperty("id").GetGuid();

        using var updateResponse = await SendAuthorizedAsync(
            HttpMethod.Put, $"/api/v1/career-paths/{careerPathId}", contentManagerToken,
            new
            {
                title = $"Security Career {suffix}",
                description = "Published career path.",
                recommendedSkills = "Security, networking, incident response",
                status = "Published",
                recommendedProgramIds = Array.Empty<Guid>(),
                recommendedCourseIds = Array.Empty<Guid>(),
            });
        updateResponse.EnsureSuccessStatusCode();

        using var deleteResponse = await SendAuthorizedAsync(
            HttpMethod.Delete, $"/api/v1/career-paths/{careerPathId}", contentManagerToken);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task CreateCourse_WithUnknownCategory_ReturnsValidationProblem()
    {
        var adminToken = await RegisterAsAsync(RoleNames.Admin);

        using var response = await SendAuthorizedAsync(
            HttpMethod.Post, "/api/v1/courses", adminToken,
            new
            {
                title = $"Orphan Course {Guid.NewGuid():N}",
                categoryId = Guid.NewGuid(),
                description = "desc",
                status = "Published",
                type = "Online",
                price = (decimal?)null,
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateCourse_WithDuplicateTitle_ReturnsConflict()
    {
        var adminToken = await RegisterAsAsync(RoleNames.Admin);
        var suffix = Guid.NewGuid().ToString("N");
        var categoryId = await CreateCategoryAsync($"Duplicate Test Category {suffix}", adminToken);
        var title = $"Duplicate Course {suffix}";

        using var firstResponse = await SendAuthorizedAsync(
            HttpMethod.Post, "/api/v1/courses", adminToken,
            new { title, categoryId, description = "desc", status = "Published", type = "Online", price = (decimal?)null });
        firstResponse.EnsureSuccessStatusCode();

        using var secondResponse = await SendAuthorizedAsync(
            HttpMethod.Post, "/api/v1/courses", adminToken,
            new { title, categoryId, description = "desc 2", status = "Published", type = "Online", price = (decimal?)null });

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task DraftCourse_IsHiddenFromPublicEndpoints_ButVisibleToAuthorizedCatalogEditors()
    {
        var adminToken = await RegisterAsAsync(RoleNames.Admin);
        var suffix = Guid.NewGuid().ToString("N");
        var categoryId = await CreateCategoryAsync($"Draft Visibility Category {suffix}", adminToken);
        var title = $"Draft Visibility Course {suffix}";

        using var createResponse = await SendAuthorizedAsync(
            HttpMethod.Post, "/api/v1/courses", adminToken,
            new { title, categoryId, description = "desc", status = "Draft", type = "Online", price = (decimal?)null });
        createResponse.EnsureSuccessStatusCode();
        using var createDocument = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var slug = createDocument.RootElement.GetProperty("slug").GetString()!;

        using var publicListResponse = await _client.GetAsync($"/api/v1/courses?search={Uri.EscapeDataString(title)}");
        publicListResponse.EnsureSuccessStatusCode();
        using var publicListDocument = JsonDocument.Parse(await publicListResponse.Content.ReadAsStringAsync());
        Assert.Empty(publicListDocument.RootElement.GetProperty("items").EnumerateArray());

        using var publicSlugResponse = await _client.GetAsync($"/api/v1/courses/{slug}");
        Assert.Equal(HttpStatusCode.NotFound, publicSlugResponse.StatusCode);

        using var filteredListResponse = await _client.GetAsync(
            $"/api/v1/courses?search={Uri.EscapeDataString(title)}&status=Draft");
        filteredListResponse.EnsureSuccessStatusCode();
        using var filteredListDocument = JsonDocument.Parse(await filteredListResponse.Content.ReadAsStringAsync());
        Assert.Empty(filteredListDocument.RootElement.GetProperty("items").EnumerateArray());

        using var managementListResponse = await SendAuthorizedAsync(
            HttpMethod.Get,
            $"/api/v1/courses/manage?search={Uri.EscapeDataString(title)}&status=Draft",
            adminToken);
        managementListResponse.EnsureSuccessStatusCode();
        using var managementListDocument = JsonDocument.Parse(await managementListResponse.Content.ReadAsStringAsync());
        Assert.Single(managementListDocument.RootElement.GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task UpdateCourse_ChangesFieldsAndPersists()
    {
        var adminToken = await RegisterAsAsync(RoleNames.Admin);
        var suffix = Guid.NewGuid().ToString("N");
        var categoryId = await CreateCategoryAsync($"Update Test Category {suffix}", adminToken);

        using var createResponse = await SendAuthorizedAsync(
            HttpMethod.Post, "/api/v1/courses", adminToken,
            new
            {
                title = $"Update Test Course {suffix}",
                categoryId,
                description = "Original description.",
                status = "Draft",
                type = "Online",
                price = (decimal?)null,
            });
        createResponse.EnsureSuccessStatusCode();
        using var createDocument = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var courseId = createDocument.RootElement.GetProperty("id").GetGuid();

        using var updateResponse = await SendAuthorizedAsync(
            HttpMethod.Put, $"/api/v1/courses/{courseId}", adminToken,
            new
            {
                title = $"Updated Course {suffix}",
                categoryId,
                description = "Updated description.",
                status = "Published",
                type = "Hybrid",
                price = 75.50m,
            });
        updateResponse.EnsureSuccessStatusCode();
        using var updateDocument = JsonDocument.Parse(await updateResponse.Content.ReadAsStringAsync());
        Assert.Equal($"Updated Course {suffix}", updateDocument.RootElement.GetProperty("title").GetString());
        Assert.Equal("Published", updateDocument.RootElement.GetProperty("status").GetString());
        Assert.Equal("Hybrid", updateDocument.RootElement.GetProperty("type").GetString());
        Assert.Equal(75.50m, updateDocument.RootElement.GetProperty("price").GetDecimal());
    }

    [Fact]
    public async Task CreateCategory_WithDuplicateName_ReturnsConflict()
    {
        var adminToken = await RegisterAsAsync(RoleNames.Admin);
        var name = $"Duplicate Category {Guid.NewGuid():N}";

        await CreateCategoryAsync(name, adminToken);

        using var response = await SendAuthorizedAsync(
            HttpMethod.Post, "/api/v1/course-categories", adminToken, new { name });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private async Task<Guid> CreateCategoryAsync(string name, string adminToken)
    {
        using var response = await SendAuthorizedAsync(
            HttpMethod.Post, "/api/v1/course-categories", adminToken, new { name });
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("id").GetGuid();
    }

    private async Task<HttpResponseMessage> SendAuthorizedAsync(
        HttpMethod method, string url, string accessToken, object? body = null)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await _client.SendAsync(request);
    }

    private async Task<string> RegisterAsAsync(string role)
    {
        var email = $"{Guid.NewGuid()}@example.com";
        const string password = "P@ssw0rd123!";

        using var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password,
            firstName = "Catalog",
            lastName = "Staff",
            phoneNumber = "+201234567890",
            countryCode = "EG",
            preferredLanguage = "en",
        });
        registerResponse.EnsureSuccessStatusCode();

        using (var scope = factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = (await userManager.FindByEmailAsync(email))!;
            await userManager.AddToRoleAsync(user, role);
        }

        using var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        loginResponse.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("accessToken").GetString()!;
    }
}
