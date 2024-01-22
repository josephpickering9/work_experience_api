using Work_Experience_Search.Exceptions;

namespace Work_Experience_Search.Services;

using Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Controllers;

public class ProjectService : IProjectService
{
    private readonly Database _context;
    private readonly IFileService _fileService;

    public ProjectService(Database context, IFileService fileService)
    {
        _context = context;
        _fileService = fileService;
    }

    public async Task<IEnumerable<Project>> GetProjectsAsync(string? search)
    {
        IQueryable<Project> projects = _context.Project;

        if (!String.IsNullOrEmpty(search))
        {
            projects = projects.Where(p => p.Title.ToLower().Contains(search.ToLower()) || p.Description.ToLower().Contains(search.ToLower()));
        }

        return await projects.ToListAsync();
    }

    public async Task<Project> GetProjectAsync(int id)
    {
        Project? project = await _context.Project.FindAsync(id);
        if (project == null)
        {
            throw new NotFoundException("Project not found.");
        }
        
        if (project != null)
        {
            project.Tags = await _context.Tag.Where(t => t.Projects.Any(p => p.Id == project.Id)).ToListAsync();
        }

        return project;
    }

    public async Task<Project> CreateProjectAsync(CreateProject createProject)
    {
        bool projectExists = await _context.Project
            .AnyAsync(p => p.Title.ToLower() == createProject.Title.ToLower());

        if (projectExists)
        {
            throw new ConflictException("A project with the same title already exists");
        }

        string? imagePath = createProject.Image != null ? Path.GetFileName(await _fileService.SaveFileAsync(createProject.Image)) : null;
        string? bgImagePath = createProject.BackgroundImage != null ? Path.GetFileName(await _fileService.SaveFileAsync(createProject.BackgroundImage)) : null;

        Project project = new Project
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

        _context.Project.Add(project);
        await _context.SaveChangesAsync();

        if (createProject.Tags != null && createProject.Tags.Count > 0)
        {
            foreach (string tagTitle in createProject.Tags)
            {
                Tag? tag = await _context.Tag.FirstOrDefaultAsync(t => t.Title.ToLower() == tagTitle.ToLower());

                if (tag == null)
                {
                    tag = new Tag
                    {
                        Title = tagTitle,
                        Type = TagType.Default,
                        Colour = "blue",
                    };

                    _context.Tag.Add(tag);
                }

                project.Tags.Add(tag);
            }

            await _context.SaveChangesAsync();
        }

        return project;
    }


    public async Task<Project> UpdateProjectAsync(int id, CreateProject createProject)
    {
        Project? project = await _context.Project.FindAsync(id);
        if (project == null)
        {
            throw new NotFoundException("Project not found.");
        }

        bool projectExists = await _context.Project
            .AnyAsync(p => p.Title.ToLower() == createProject.Title.ToLower());

        if (projectExists)
        {
            throw new ConflictException("A project with the same title already exists");
        }
        
        string? imagePath = createProject.Image != null ? Path.GetFileName(await _fileService.SaveFileAsync(createProject.Image)) : null;
        string? bgImagePath = createProject.BackgroundImage != null ? Path.GetFileName(await _fileService.SaveFileAsync(createProject.BackgroundImage)) : null;

        if (imagePath != null)
        {
            project.Image = imagePath;
        }

        if (bgImagePath != null)
        {
            project.BackgroundImage = bgImagePath;
        }

        project.Title = createProject.Title;
        project.ShortDescription = createProject.ShortDescription;
        project.Description = createProject.Description;
        project.Company = createProject.Company;
        project.Year = createProject.Year;
        project.Website = createProject.Website;

        _context.Entry(project).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        
        if (createProject.Tags.Count > 0)
        {
            List<Tag> newTags = new List<Tag>();
            
            foreach (string tagTitle in createProject.Tags)
            {
                Tag? tag = await _context.Tag.FirstOrDefaultAsync(t => t.Title.ToLower() == tagTitle.ToLower());
                if (tag == null)
                {
                    tag = new Tag
                    {
                        Title = tagTitle,
                        Type = TagType.Default,
                        Colour = "blue",
                    };

                    _context.Tag.Add(tag);
                }
                
                newTags.Add(tag);
            }

            project.Tags = newTags;
            await _context.SaveChangesAsync();
        }

        return project;
    }


    public async Task<Project> DeleteProjectAsync(int id)
    {
        Project project = await _context.Project.FindAsync(id);
        if (project == null)
        {
            throw new NotFoundException("Project not found.");
        }

        _context.Project.Remove(project);
        await _context.SaveChangesAsync();

        return project;
    }
}
