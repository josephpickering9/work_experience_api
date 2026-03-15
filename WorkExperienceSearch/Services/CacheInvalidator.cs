using Microsoft.Extensions.Primitives;

namespace Work_Experience_Search.Services;

public class CacheInvalidator
{
    private CancellationTokenSource _projectsToken = new();
    private CancellationTokenSource _companiesToken = new();
    private CancellationTokenSource _tagsToken = new();

    public IChangeToken GetProjectsChangeToken() =>
        new CancellationChangeToken(_projectsToken.Token);

    public IChangeToken GetCompaniesChangeToken() =>
        new CancellationChangeToken(_companiesToken.Token);

    public IChangeToken GetTagsChangeToken() =>
        new CancellationChangeToken(_tagsToken.Token);

    public void InvalidateProjects()
    {
        var cts = _projectsToken;
        _projectsToken = new CancellationTokenSource();
        cts.Cancel();
        cts.Dispose();
    }

    public void InvalidateCompanies()
    {
        var cts = _companiesToken;
        _companiesToken = new CancellationTokenSource();
        cts.Cancel();
        cts.Dispose();
    }

    public void InvalidateTags()
    {
        var cts = _tagsToken;
        _tagsToken = new CancellationTokenSource();
        cts.Cancel();
        cts.Dispose();
    }
}
