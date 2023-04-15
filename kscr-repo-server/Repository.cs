using System.Collections.Concurrent;

namespace KScr.Server.Repo;

public class Repository
{
}

internal interface IRepoNode {}

internal class RepoGroupNode : ConcurrentDictionary<string, IRepoNode>, IRepoNode
{
}
