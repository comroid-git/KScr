using KScr.Build;
using Microsoft.AspNetCore.Mvc;
using NAutowired.Core.Attributes;

namespace KScr.Server.Repo.Controllers;

[ApiController]
[Route("[controller]")]
public class RepositoryController : ControllerBase
{
    [Autowired]
    private readonly Repository _repository;
    
    [HttpGet, Route("module/{domain}/{group}/{id}/{version}")]
    public IEnumerable<DependencyInfo> Get(string domain, string group, string id, string version)
    {
    }
}