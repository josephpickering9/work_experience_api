using Microsoft.EntityFrameworkCore;
using Moq;
using Work_Experience_Search.Controllers;
using Work_Experience_Search.Exceptions;
using Work_Experience_Search.Models;

namespace Work_Experience_Search.Services;

public class ProjectService : IProjectService
{
    private readonly Database _context;
    private readonly IProjectImageService _projectImageService;
    private readonly ITagService _tagService;

    public ProjectService(Database context, IProjectImageService projectImageService, ITagService tagService)
    {
        _context = context;
        _projectImageService = projectImageService;
        _tagService = tagService;
    }

    public async Task<IEnumerable<Project>> GetProjectsAsync(string? search)
    {
        IQueryable<Project> projects = _context.Project
            .Include(p => p.Tags)
            .Include(p => p.Images.OrderBy(i => i.Type).ThenBy(i => i.Order != null ? i.Order : 0));

        if (!string.IsNullOrEmpty(search))
            projects = projects.Where(p =>
                p.Title.ToLower().Contains(search.ToLower()) || p.Description.ToLower().Contains(search.ToLower()));

        return await projects.OrderByDescending(p => p.Year).ToListAsync();
    }

    public async Task<Project> GetProjectAsync(int id)
    {
        var project = await _context.Project
            .Include(p => p.Tags)
            .Include(p => p.Images.OrderBy(i => i.Type).ThenBy(i => i.Order != null ? i.Order : 0))
            .SingleOrDefaultAsync(p => p.Id == id);
        if (project == null) throw new NotFoundException("Project not found.");

        return project;
    }

    public async Task<Project> GetProjectBySlugAsync(string slug)
    {
        var project = await _context.Project
            .Include(p => p.Tags)
            .Include(p => p.Images.OrderBy(i => i.Type).ThenBy(i => i.Order != null ? i.Order : 0))
            .FirstOrDefaultAsync(p => p.Slug == slug);
        if (project == null) throw new NotFoundException("Project not found.");

        return project;
    }

    public async Task<IEnumerable<Project>> GetRelatedProjectsAsync(int projectId)
    {
        var projectTags = _context.Project
            .Include(pt => pt.Tags)
            .Where(pt => pt.Id == projectId)
            .SelectMany(pt => pt.Tags.Select(t => t.Id));

        if (!projectTags.Any()) return new List<Project>();

        var relatedProjects = await _context.Project
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

        return relatedProjects;
    }

    public async Task<Project> CreateProjectAsync(CreateProject createProject)
    {
        var projectExists = await _context.Project
            .AnyAsync(p => p.Title.ToLower() == createProject.Title.ToLower());

        if (projectExists) throw new ConflictException("A project with the same title already exists");

        var project = new Project
        {
            Title = createProject.Title,
            ShortDescription = createProject.ShortDescription,
            Description = createProject.Description,
            CompanyId = createProject.CompanyId,
            Year = createProject.Year,
            Website = createProject.Website,
            Slug = createProject.Title.ToSlug(),
            Tags = new List<Tag>()
        };

        if (createProject.Tags.Count > 0) project.Tags = await _tagService.SyncTagsAsync(createProject.Tags);

        _context.Project.Add(project);
        await _context.SaveChangesAsync();

        if (createProject.Images.Count > 0)
            project.Images = await _projectImageService.SyncProjectImagesAsync(project, createProject.Images);

        await _context.SaveChangesAsync();

        return project;
    }

    public async Task<Project> UpdateProjectAsync(int id, CreateProject createProject)
    {
        var project = await GetProjectAsync(id);

        var projectExists = await _context.Project
            .AnyAsync(p => p.Id != project.Id && p.Title.ToLower() == createProject.Title.ToLower());

        if (projectExists) throw new ConflictException("A project with the same title already exists");

        project.Title = createProject.Title;
        project.ShortDescription = createProject.ShortDescription;
        project.Description = createProject.Description;
        project.CompanyId = createProject.CompanyId;
        project.Year = createProject.Year;
        project.Website = createProject.Website;
        project.Slug = createProject.Title.ToSlug();

        if (createProject.Tags.Count > 0)
        {
            project.Tags = await _tagService.SyncTagsAsync(createProject.Tags);
            await _context.SaveChangesAsync();
        }

        if (createProject.Images.Count > 0)
        {
            project.Images = await _projectImageService.SyncProjectImagesAsync(project, createProject.Images);
            await _context.SaveChangesAsync();
        }

        _context.Entry(project).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return project;
    }


    public async Task<Project> DeleteProjectAsync(int id)
    {
        var project = await _context.Project.FindAsync(id);
        if (project == null) throw new NotFoundException("Project not found.");

        _context.Project.Remove(project);
        await _context.SaveChangesAsync();

        return project;
    }

    private class TagComparer : IEqualityComparer<Tag>
    {
        public bool Equals(Tag x, Tag y)
        {
            return x.Id == y.Id;
        }

        public int GetHashCode(Tag obj)
        {
            return obj.Id.GetHashCode();
        }
    }
}
