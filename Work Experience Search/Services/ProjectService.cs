using Microsoft.EntityFrameworkCore;
using Work_Experience_Search.Controllers;
using Work_Experience_Search.Exceptions;
using Work_Experience_Search.Models;

namespace Work_Experience_Search.Services;

public class ProjectService : IProjectService
{
    private readonly Database _context;
    private readonly IFileService _fileService;
    private readonly ITagService _tagService;

    public ProjectService(Database context, IFileService fileService, ITagService tagService)
    {
        _context = context;
        _fileService = fileService;
        _tagService = tagService;
    }

    public async Task<IEnumerable<Project>> GetProjectsAsync(string? search)
    {
        IQueryable<Project> projects = _context.Project.Include(p => p.Tags);

        if (!string.IsNullOrEmpty(search))
            projects = projects.Where(p =>
                p.Title.ToLower().Contains(search.ToLower()) || p.Description.ToLower().Contains(search.ToLower()));

        return await projects.OrderByDescending(p => p.Year).ToListAsync();
    }

    public async Task<Project> GetProjectAsync(int id)
    {
        var project = await _context.Project.Include(p => p.Tags).SingleOrDefaultAsync(p => p.Id == id);
        if (project == null) throw new NotFoundException("Project not found.");

        return project;
    }

    public async Task<Project> CreateProjectAsync(CreateProject createProject)
    {
        var projectExists = await _context.Project
            .AnyAsync(p => p.Title.ToLower() == createProject.Title.ToLower());

        if (projectExists) throw new ConflictException("A project with the same title already exists");

        var imagePath = createProject.Image != null
            ? Path.GetFileName(await _fileService.SaveFileAsync(createProject.Image))
            : null;
        var bgImagePath = createProject.BackgroundImage != null
            ? Path.GetFileName(await _fileService.SaveFileAsync(createProject.BackgroundImage))
            : null;

        var project = new Project
        {
            Title = createProject.Title,
            ShortDescription = createProject.ShortDescription,
            Description = createProject.Description,
            Company = createProject.Company,
            Image = imagePath,
            BackgroundImage = bgImagePath,
            Year = createProject.Year,
            Website = createProject.Website,
            Tags = new List<Tag>()
        };

        if (createProject.Tags.Count > 0)
        {
            project.Tags = await _tagService.SyncTagsAsync(createProject.Tags);
        }

        _context.Project.Add(project);
        await _context.SaveChangesAsync();

        return project;
    }


    public async Task<Project> UpdateProjectAsync(int id, CreateProject createProject)
    {
        var project = await GetProjectAsync(id);

        var projectExists = await _context.Project
            .AnyAsync(p => p.Id != project.Id && p.Title.ToLower() == createProject.Title.ToLower());

        if (projectExists) throw new ConflictException("A project with the same title already exists");

        var imagePath = createProject.Image != null
            ? Path.GetFileName(await _fileService.SaveFileAsync(createProject.Image))
            : null;
        var bgImagePath = createProject.BackgroundImage != null
            ? Path.GetFileName(await _fileService.SaveFileAsync(createProject.BackgroundImage))
            : null;

        if (imagePath != null) project.Image = imagePath;

        if (bgImagePath != null) project.BackgroundImage = bgImagePath;

        project.Title = createProject.Title;
        project.ShortDescription = createProject.ShortDescription;
        project.Description = createProject.Description;
        project.Company = createProject.Company;
        project.Year = createProject.Year;
        project.Website = createProject.Website;

        if (createProject.Tags.Count > 0)
        {
            project.Tags = await _tagService.SyncTagsAsync(createProject.Tags);
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

    private void UpdateProjectTags(Project project, List<Tag> newTags)
    {
        project.Tags.Clear();
        project.Tags.AddRange(newTags);

        // var tagsToAdd = newTags.Except(project.Tags).ToList();
        // var tagsToRemove = project.Tags.Except(newTags).Select(tag => tag.Id).ToList();
        //
        // if (tagsToAdd.Count > 0) project.Tags.AddRange(tagsToAdd);
        // if (tagsToRemove.Count > 0) project.Tags.RemoveAll(tag => tagsToRemove.Contains(tag.Id));
    }
}
