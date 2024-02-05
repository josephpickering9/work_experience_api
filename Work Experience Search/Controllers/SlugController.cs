using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Work_Experience_Search.Exceptions;
using Work_Experience_Search.Models;
using Work_Experience_Search.Services;

namespace Work_Experience_Search.Controllers;

[ApiController]
[Route("[controller]")]
public class SlugController : ControllerBase
{
    private readonly ITagService _tagService;
    private readonly ICompanyService _companyService;
    private readonly IProjectService _projectService;

    public SlugController(ITagService tagService, ICompanyService companyService, IProjectService projectService)
    {
        _tagService = tagService;
        _companyService = companyService;
        _projectService = projectService;
    }

    [HttpPost]
    public async Task PostSlug()
    {
        try
        {
            var tags = await _tagService.GetTagsAsync(null);
            foreach (var tag in tags)
            {
                if (string.IsNullOrEmpty(tag.Slug))
                {
                    var createTag = new CreateTag
                    {
                        Title = tag.Title,
                        Type = tag.Type,
                        Icon = tag.Icon,
                        CustomColour = tag.CustomColour,
                    };
                    await _tagService.UpdateTagAsync(tag.Id, createTag);
                }
            }

            var companies = await _companyService.GetCompaniesAsync(null);
            foreach (var company in companies)
            {
                if (string.IsNullOrEmpty(company.Slug))
                {
                    var createCompany = new CreateCompany
                    {
                        Name = company.Name,
                        Description = company.Description,
                        Website = company.Website,
                    };
                    await _companyService.UpdateCompanyAsync(company.Id, createCompany);
                }
            }

            var projects = await _projectService.GetProjectsAsync(null);
            foreach (var project in projects)
            {
                if (string.IsNullOrEmpty(project.Slug))
                {
                    var createProject = new CreateProject
                    {
                        Title = project.Title,
                        ShortDescription = project.ShortDescription,
                        Description = project.Description,
                        CompanyId = project.CompanyId,
                        Year = project.Year,
                        Website = project.Website,
                    };
                    await _projectService.UpdateProjectAsync(project.Id, createProject);
                }
            }
        }
        catch (InvalidOperationException e)
        {

        }
    }
}
