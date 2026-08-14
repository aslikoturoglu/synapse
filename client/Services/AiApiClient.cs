using System.Net.Http.Json;

namespace Client.Services;

// The note-creation half of the AI pipeline — mirrors server/Controllers/AiController.cs.
// Takes plain (name, bytes) tuples rather than a Pages-layer file type, keeping this service
// layer independent of the wizard's own models (NotesStore does that mapping).
public class AiApiClient(HttpClient http)
{
    public async Task<OrchestratorStartResponse?> StartOrchestratorAsync(IReadOnlyList<(string Name, byte[] Bytes)> files, string description)
    {
        using var content = new MultipartFormDataContent();
        foreach (var file in files)
            content.Add(new ByteArrayContent(file.Bytes), "Files", file.Name);
        content.Add(new StringContent(description), "Description");

        var response = await http.PostAsync("api/ai/orchestrator/start", content);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<OrchestratorStartResponse>() : null;
    }

    public async Task<NoteAiDirectiveDto?> SelectOrchestratorMenuAsync(string threadId, string selection)
    {
        var response = await http.PostAsJsonAsync("api/ai/orchestrator/select", new OrchestratorSelectRequest { ThreadId = threadId, Selection = selection });
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<NoteAiDirectiveDto>() : null;
    }

    public async Task<GenerateNoteResponse?> GenerateNoteAsync(GenerateNoteRequest request)
    {
        var response = await http.PostAsJsonAsync("api/ai/generate-note", request);
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<GenerateNoteResponse>() : null;
    }
}
