﻿using Microsoft.EntityFrameworkCore;
using Work_Experience_Search.Controllers;
using Work_Experience_Search.Exceptions;
using Work_Experience_Search.Models;

namespace Work_Experience_Search.Services;

public class CompanyService : ICompanyService
{
    private readonly Database _context;
    private readonly IFileService _fileService;

    public CompanyService(Database context, IFileService fileService)
    {
        _context = context;
        _fileService = fileService;
    }

    public async Task<IEnumerable<Company>> GetCompaniesAsync(string? search)
    {
        IQueryable<Company> companies = _context.Company;

        if (!string.IsNullOrEmpty(search))
            companies = companies.Where(p => p.Name.ToLower().Contains(search.ToLower()));

        return await companies.ToListAsync();
    }

    public async Task<Company> GetCompanyAsync(int id)
    {
        var company = await _context.Company.FindAsync(id);
        if (company == null) throw new NotFoundException("Company not found.");

        return company;
    }

    public async Task<Company> CreateCompanyAsync(CreateCompany createCompany)
    {
        var companyExists = await _context.Company
            .AnyAsync(p => p.Name.ToLower() == createCompany.Name.ToLower());

        if (companyExists) throw new ConflictException("A company with the same title already exists");

        var logoPath = createCompany.Logo != null
            ? Path.GetFileName(await _fileService.SaveFileAsync(createCompany.Logo))
            : null;

        var company = new Company
        {
            Name = createCompany.Name,
            Description = createCompany.Description,
            Logo = logoPath,
            Website = createCompany.Website,
        };

        _context.Company.Add(company);
        await _context.SaveChangesAsync();

        return company;
    }

    public async Task<Company> UpdateCompanyAsync(int id, CreateCompany createCompany)
    {
        var company = await _context.Company.FindAsync(id);
        if (company == null) throw new NotFoundException("Company not found.");

        var companyExists = await _context.Company
            .AnyAsync(p => p.Id != company.Id && p.Name.ToLower() == createCompany.Name.ToLower());

        if (companyExists) throw new ConflictException("A company with the same title already exists");

        var logoPath = createCompany.Logo != null
            ? Path.GetFileName(await _fileService.SaveFileAsync(createCompany.Logo))
            : null;

        if (logoPath != null) company.Logo = logoPath;

        company.Name = createCompany.Name;
        company.Description = createCompany.Description;
        company.Website = createCompany.Website;

        await _context.SaveChangesAsync();

        return company;
    }


    public async Task<Company> DeleteCompanyAsync(int id)
    {
        var company = await _context.Company.FindAsync(id);
        if (company == null) throw new NotFoundException("Company not found.");

        _context.Company.Remove(company);
        await _context.SaveChangesAsync();

        return company;
    }
}
