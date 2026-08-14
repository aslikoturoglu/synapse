using Microsoft.AspNetCore.Components.Forms;

namespace Client.Pages.CreateNote.Upload;

// Shared by UploadStep's initial dropzone and FileGrid's "add more" tile — both need to turn
// a freshly-picked IBrowserFile into real bytes stored on the Draft (not just its name), since
// the Orchestrator step needs to actually upload file content to Azure AI Foundry.
public static class UploadedFileReader
{
    // Above the SDK's own per-agent limits, but large enough for a real lecture-note PDF.
    private const long MaxFileBytes = 25_000_000;

    public static async Task AddFilesAsync(NoteDraft draft, IEnumerable<IBrowserFile> files)
    {
        foreach (var file in files)
        {
            byte[] bytes;
            try
            {
                await using var stream = file.OpenReadStream(MaxFileBytes);
                using var memory = new MemoryStream();
                await stream.CopyToAsync(memory);
                bytes = memory.ToArray();
            }
            catch (IOException)
            {
                // Over the size limit — skip it rather than crash the wizard.
                continue;
            }

            draft.FileNames.Add(file.Name);
            draft.Files.Add(new UploadedFileDraft { Name = file.Name, Bytes = bytes });
        }
    }
}
