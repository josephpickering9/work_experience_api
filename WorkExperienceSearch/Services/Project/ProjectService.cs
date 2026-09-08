using Work_Experience_Search.Models;
using Work_Experience_Search.Repositories;
using Work_Experience_Search.Requests;
using Work_Experience_Search.Types;
using Work_Experience_Search.Utils;

namespace Work_Experience_Search.Services;

public class ProjectService(
    IProjectRepository projectRepository,
    IProjectImageService projectImageService,
    IProjectRepositoryService projectRepositoryService,
    ITagService tagService
) : IProjectService
{
    public async Task<Result<IEnumerable<Project>>> GetProjectsAsync(string? search)
    {
        var projects = await projectRepository.SearchAsync(search);
        return new Success<IEnumerable<Project>>(projects);
    }

    public async Task<Result<Project>> GetProjectAsync(ProjectId id)
    {
        var project = await projectRepository.GetAsync(id);
        if (project == null) return new NotFoundFailure<Project>("Project not found.");

        return new Success<Project>(project);
    }

    public async Task<Result<Project>> GetProjectBySlugAsync(string slug)
    {
        var project = await projectRepository.GetAsync(slug);
        if (project == null) return new NotFoundFailure<Project>("Project not found.");

        return new Success<Project>(project);
    }

    public async Task<Result<IEnumerable<Project>>> GetRelatedProjectsAsync(ProjectId projectId)
    {
        var projects = await projectRepository.GetRelatedAsync(projectId);
        return new Success<IEnumerable<Project>>(projects);
    }

    public async Task<Result<Project>> CreateProjectAsync(CreateProject createProject)
    {
        var projectExists = await projectRepository.ExistsAsync(createProject.Title);
        if (projectExists) return new ConflictFailure<Project>("A project with the same title already exists");

        var project = new Project
        {
            Title = createProject.Title,
            ShortDescription = createProject.ShortDescription,
            Description = createProject.Description,
            CompanyId = createProject.CompanyId,
            StartDate = createProject.StartDate,
            EndDate = createProject.EndDate,
            Website = createProject.Website,
            ShowMockup = createProject.ShowMockup,
            Slug = createProject.Title.ToSlug()
        };

        await projectRepository.AddAsync(project);

        var relationsResult = await SyncProjectRelations(project, createProject);
        if (!relationsResult.IsSuccess)
        {
            await projectRepository.DeleteAsync(project);
            return relationsResult;
        }

        await projectRepository.SaveChangesAsync();

        return new Success<Project>(project);
    }

    public async Task<Result<Project>> UpdateProjectAsync(ProjectId id, CreateProject createProject)
    {
        var project = await projectRepository.GetForUpdateAsync(id);
        if (project == null) return new NotFoundFailure<Project>("Project not found.");

        var projectExists = await projectRepository.ExistsAsync(createProject.Title, id);
        if (projectExists) return new ConflictFailure<Project>("A project with the same title already exists");

        project.Title = createProject.Title;
        project.ShortDescription = createProject.ShortDescription;
        project.Description = createProject.Description;
        project.CompanyId = createProject.CompanyId;
        project.StartDate = createProject.StartDate;
        project.EndDate = createProject.EndDate;
        project.Website = createProject.Website;
        project.ShowMockup = createProject.ShowMockup;
        project.Slug = createProject.Title.ToSlug();

        var relationsResult = await SyncProjectRelations(project, createProject);
        if (!relationsResult.IsSuccess) return relationsResult;

        await projectRepository.UpdateAsync(project);

        return new Success<Project>(project);
    }

    public async Task<Result<Project>> DeleteProjectAsync(ProjectId id)
    {
        var project = await projectRepository.GetAsync(id);
        if (project == null) return new NotFoundFailure<Project>("Project not found.");

        await projectRepository.DeleteAsync(project);

        return new Success<Project>(project);
    }

    private async Task<Result<Project>> SyncProjectRelations(Project project, CreateProject createProject)
    {
        var tagsResult = await SyncProjectTags(project, createProject);
        if (!tagsResult.IsSuccess) return tagsResult;

        var imagesResult = await SyncProjectImages(project, createProject);
        if (!imagesResult.IsSuccess) return imagesResult;

        var repositoriesResult = await SyncProjectRepositories(project, createProject);
        if (!repositoriesResult.IsSuccess) return repositoriesResult;

        return new Success<Project>(project);
    }

    private async Task<Result<Project>> SyncProjectTags(Project project, CreateProject createProject)
    {
        var tags = await tagService.SyncTagsAsync(createProject.Tags);
        if (!tags.IsSuccess || tags.Data == null) return new BadRequestFailure<Project>("Tags could not be created");

        var projectTagsToDelete = project.Tags.Where(t => tags.Data.All(x => x.Id != t.Id)).ToList();
        foreach (var tag in projectTagsToDelete)
        {
            project.Tags.Remove(tag);
        }

        project.Tags = tags.Data;

        return new Success<Project>(project);
    }

    private async Task<Result<Project>> SyncProjectImages(Project project, CreateProject createProject)
    {
        var images = await projectImageService.SyncProjectImagesAsync(project, createProject.Images);
        if (!images.IsSuccess || images.Data == null) return new BadRequestFailure<Project>("Images could not be created");

        project.Images = images.Data;

        return new Success<Project>(project);
    }

    private async Task<Result<Project>> SyncProjectRepositories(Project project, CreateProject createProject)
    {
        var repositories = await projectRepositoryService.SyncProjectRepositoriesAsync(project, createProject.Repositories);
        if (!repositories.IsSuccess || repositories.Data == null) return new BadRequestFailure<Project>("Repositories could not be created");

        project.Repositories = repositories.Data;

        return new Success<Project>(project);
    }
}
