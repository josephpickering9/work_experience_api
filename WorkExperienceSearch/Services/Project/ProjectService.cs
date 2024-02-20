using Microsoft.EntityFrameworkCore;
using Work_Experience_Search.Controllers;
using Work_Experience_Search.Models;
using Work_Experience_Search.Types;
using Work_Experience_Search.Utils;

namespace Work_Experience_Search.Services;

public class ProjectService(
    Database context,
    IProjectImageService projectImageService,
    IProjectRepositoryService projectRepositoryService,
    ITagService tagService
) : IProjectService
{
    public async Task<Result<IEnumerable<Project>>> GetProjectsAsync(string? search)
    {
        var projects = await GetProjectsQuery(search).ToListAsync();
        return new Success<IEnumerable<Project>>(projects);
    }

    public async Task<Result<Project>> GetProjectAsync(int id)
    {
        var project = await GetProjectsQuery().SingleOrDefaultAsync(p => p.Id == id);
        if (project == null) return new NotFoundFailure<Project>("Project not found.");

        return new Success<Project>(project);
    }

    public async Task<Result<Project>> GetProjectBySlugAsync(string slug)
    {
        var project = await GetProjectsQuery().FirstOrDefaultAsync(p => p.Slug == slug);
        if (project == null) return new NotFoundFailure<Project>("Project not found.");

        return new Success<Project>(project);
    }

    public async Task<Result<IEnumerable<Project>>> GetRelatedProjectsAsync(int projectId)
    {
        var projectTags = context.Project
            .Include(pt => pt.Tags)
            .Where(pt => pt.Id == projectId)
            .SelectMany(pt => pt.Tags.Select(t => t.Id));
        if (!projectTags.Any()) return new Success<IEnumerable<Project>>(new List<Project>());

        var relatedProjects = await context.Project
            .Where(p => p.Id != projectId && p.Tags.Any(t => projectTags.Contains(t.Id)))
            .Select(p => new
            {
                Project = p,
                SharedTagsCount = p.Tags.Count(t => projectTags.Contains(t.Id))
            })
            .OrderByDescending(x => x.SharedTagsCount)
            .Take(3)
            .Select(x => x.Project)
            .Include(p => p.Tags)
            .Include(p => p.Images)
            .ToListAsync();

        return new Success<IEnumerable<Project>>(relatedProjects);
    }

    public async Task<Result<Project>> CreateProjectAsync(CreateProject createProject)
    {
        var projectExists = await context.Project.AnyAsync(p => EF.Functions.ILike(p.Title, createProject.Title));
        if (projectExists) return new ConflictFailure<Project>("A project with the same title already exists");

        var project = new Project
        {
            Title = createProject.Title,
            ShortDescription = createProject.ShortDescription,
            Description = createProject.Description,
            CompanyId = createProject.CompanyId,
            Year = createProject.Year,
            Website = createProject.Website,
            ShowMockup = createProject.ShowMockup,
            Slug = createProject.Title.ToSlug(),
            Tags = []
        };
        context.Project.Add(project);

        var tagsResult = await SyncProjectTags(project, createProject);
        if (!tagsResult.IsSuccess) return tagsResult;

        var imagesResult = await SyncProjectImages(project, createProject);
        if (!imagesResult.IsSuccess) return imagesResult;

        var repositoriesResults = await SyncProjectRepositories(project, createProject);
        if (!repositoriesResults.IsSuccess) return repositoriesResults;

        await context.SaveChangesAsync();

        return new Success<Project>(project);
    }

    public async Task<Result<Project>> UpdateProjectAsync(int id, CreateProject createProject)
    {
        var projectResult = await GetProjectAsync(id);
        if (!projectResult.IsSuccess || projectResult.Data == null) return projectResult;

        var project = projectResult.Data;
        var projectExists = await context.Project.AnyAsync(p =>
            p.Id != project.Id && EF.Functions.ILike(p.Title, createProject.Title));
        if (projectExists) return new ConflictFailure<Project>("A project with the same title already exists");

        project.Title = createProject.Title;
        project.ShortDescription = createProject.ShortDescription;
        project.Description = createProject.Description;
        project.CompanyId = createProject.CompanyId;
        project.Year = createProject.Year;
        project.Website = createProject.Website;
        project.ShowMockup = createProject.ShowMockup;
        project.Slug = createProject.Title.ToSlug();

        if (createProject.Tags.Count > 0)
        {
            var tags = await tagService.SyncTagsAsync(createProject.Tags);
            if (!tags.IsSuccess || tags.Data == null) return new BadRequestFailure<Project>("Tags could not be created");

            project.Tags = tags.Data;
            await context.SaveChangesAsync();
        }

        if (createProject.Images.Count > 0)
        {
            var images = await projectImageService.SyncProjectImagesAsync(project, createProject.Images);
            if (!images.IsSuccess || images.Data == null) return new BadRequestFailure<Project>("Images could not be created");

            project.Images = images.Data;
            await context.SaveChangesAsync();
        }

        context.Entry(project).State = EntityState.Modified;
        await context.SaveChangesAsync();

        return new Success<Project>(project);
    }

    public async Task<Result<Project>> DeleteProjectAsync(int id)
    {
        var project = await context.Project.FindAsync(id);
        if (project == null) return new NotFoundFailure<Project>("Project not found.");

        context.Project.Remove(project);
        await context.SaveChangesAsync();

        return new Success<Project>(project);
    }

    private IQueryable<Project> GetProjectsQuery(string? search = null)
    {
        IQueryable<Project> projects = context.Project
            .Include(p => p.Tags)
            .Include(p => p.Images.OrderBy(i => i.Type).ThenBy(i => i.Order ?? 0))
            .Include(p => p.Repositories.OrderBy(i => i.Order ?? 0));

        if (!string.IsNullOrEmpty(search))
            projects = projects.Where(p => DatabaseExtensions.ILike(p.Title, search) || DatabaseExtensions.ILike(p.ShortDescription, search));

        return projects;
    }

    private async Task<Result<Project>> SyncProjectTags(Project project, CreateProject createProject)
    {
        if (createProject.Tags.Count <= 0) return new Success<Project>(project);

        var tags = await tagService.SyncTagsAsync(createProject.Tags);
        if (!tags.IsSuccess || tags.Data == null) return new BadRequestFailure<Project>("Tags could not be created");

        project.Tags = tags.Data;

        return new Success<Project>(project);
    }

    private async Task<Result<Project>> SyncProjectImages(Project project, CreateProject createProject)
    {
        if (createProject.Images.Count <= 0) return new Success<Project>(project);

        var images = await projectImageService.SyncProjectImagesAsync(project, createProject.Images);
        if (!images.IsSuccess || images.Data == null) return new BadRequestFailure<Project>("Images could not be created");

        project.Images = images.Data;
        await context.SaveChangesAsync();

        return new Success<Project>(project);
    }

    private async Task<Result<Project>> SyncProjectRepositories(Project project, CreateProject createProject)
    {
        if (project.Repositories.Count <= 0) return new Success<Project>(project);

        var repositories = await projectRepositoryService.SyncProjectRepositoriesAsync(project, createProject.Repositories);
        if (!repositories.IsSuccess || repositories.Data == null) return new BadRequestFailure<Project>("Repositories could not be created");

        project.Repositories = repositories.Data;
        await context.SaveChangesAsync();

        return new Success<Project>(project);
    }
}
