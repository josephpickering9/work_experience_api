using Work_Experience_Search.controllers;
using Work_Experience_Search.models;

namespace Work_Experience_Search.Services;

public interface IProjectService
{
    Task<IEnumerable<Project>> GetProjectsAsync(string? search);
    Task<Project?> GetProjectAsync(int id);
    Task<Project> CreateProjectAsync(CreateProject createProject);
    Task<Project?> UpdateProjectAsync(int id, CreateProject createProject);
    Task<Project?> DeleteProjectAsync(int id);
}