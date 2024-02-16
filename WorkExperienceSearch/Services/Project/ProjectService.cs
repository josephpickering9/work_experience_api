using Microsoft.EntityFrameworkCore;
using Work_Experience_Search.Controllers;
using Work_Experience_Search.Models;
using Work_Experience_Search.Types;

namespace Work_Experience_Search.Services;

public class ProjectService(Database context, IProjectImageService projectImageService, ITagService tagService)
    : IProjectService
{
    public async Task<Result<IEnumerable<Project>>> GetProjectsAsync(string? search)
    {
        IQueryable<Project> projects = context.Project
            .Include(p => p.Tags)
            .Include(p => p.Images.OrderBy(i => i.Type).ThenBy(i => i.Order ?? 0));

        if (!string.IsNullOrEmpty(search))
            projects = projects.Where(p => EF.Functions.ILike(p.Title, search) || EF.Functions.ILike(p.ShortDescription, search));

        return new Success<IEnumerable<Project>>(await projects.OrderByDescending(p => p.Year).ToListAsync());
    }

    public async Task<Result<Project>> GetProjectAsync(int id)
    {
        var project = await context.Project
            .Include(p => p.Tags)
            .Include(p => p.Images.OrderBy(i => i.Type).ThenBy(i => i.Order ?? 0))
            .SingleOrDefaultAsync(p => p.Id == id);
        if (project == null) return new NotFoundFailure<Project>("Project not found.");

        return new Success<Project>(project);
    }

    public async Task<Result<Project>> GetProjectBySlugAsync(string slug)
    {
        var project = await context.Project
            .Include(p => p.Tags)
            .Include(p => p.Images.OrderBy(i => i.Type).ThenBy(i => i.Order ?? 0))
            .FirstOrDefaultAsync(p => p.Slug == slug);
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

        if (createProject.Tags.Count > 0)
        {
            var tags = await tagService.SyncTagsAsync(createProject.Tags);
            project.Tags = tags.Data;
        }

        context.Project.Add(project);
        await context.SaveChangesAsync();

        if (createProject.Images.Count > 0)
        {
            var images = await projectImageService.SyncProjectImagesAsync(project, createProject.Images);
            project.Images = images.Data;
            await context.SaveChangesAsync();
        }

        await context.SaveChangesAsync();

        return new Success<Project>(project);
    }

    public async Task<Result<Project>> UpdateProjectAsync(int id, CreateProject createProject)
    {
        var projectResult = await GetProjectAsync(id);
        if (!projectResult.IsSuccess) return projectResult;

        var project = projectResult.Data;
        var projectExists = await context.Project.AnyAsync(p => p.Id != project.Id && EF.Functions.ILike(p.Title, createProject.Title));
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
            project.Tags = tags.Data;
            await context.SaveChangesAsync();
        }

        if (createProject.Images.Count > 0)
        {
            var images = await projectImageService.SyncProjectImagesAsync(project, createProject.Images);
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
}
