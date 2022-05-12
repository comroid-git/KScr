using System.Collections.Concurrent;
using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Window;

namespace KScr.LangServer;

public class KScrFileSyncHandler : ITextDocumentSyncHandler
{
    private readonly ILanguageServer _router;
    private readonly BufferManager _bufferManager;

    private readonly DocumentSelector _documentSelector = new DocumentSelector(
        new DocumentFilter()
        {
            Pattern = "**/*.csproj"
        }
    );

    private SynchronizationCapability _capability;

    public KScrFileSyncHandler(ILanguageServer router, BufferManager bufferManager)
    {
        _router = router;
        _bufferManager = bufferManager;
    }

    public TextDocumentSyncKind Change { get; } = TextDocumentSyncKind.Full;

    public TextDocumentChangeRegistrationOptions GetRegistrationOptions()
    {
        return new TextDocumentChangeRegistrationOptions()
        {
            DocumentSelector = _documentSelector,
            SyncKind = Change
        };
    }

    public TextDocumentAttributes GetTextDocumentAttributes(Uri uri)
    {
        return new TextDocumentAttributes(uri, "xml");
    }

    public Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
    {
        var documentPath = request.TextDocument.Uri.ToString();
        var text = request.ContentChanges.FirstOrDefault()?.Text;

        _bufferManager.UpdateBuffer(documentPath, text);

        _router.Window.LogInfo($"Updated buffer for document: {documentPath}\n{text}");

        return Unit.Task;
    }

    public Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
    {
        _bufferManager.UpdateBuffer(request.TextDocument.Uri.ToString(), request.TextDocument.Text);
        return Unit.Task;
    }
}

public class BufferManager
{
    private ConcurrentDictionary<string, string> _buffers = new();

    public void UpdateBuffer(string documentPath, string buffer)
    {
        _buffers.AddOrUpdate(documentPath, buffer, (k, v) => buffer);
    }

    public string? GetBuffer(string documentPath)
    {
        return _buffers.TryGetValue(documentPath, out var buffer) ? buffer : null;
    }
}