namespace Client.Services;

public record TourStep(string TargetId, string Title, string Text);

// One entry per screen — the "main tutorial" Drawer's "Learn Synapse." button replays is just
// the "home" tour, re-triggered regardless of whether it's already been seen.
public static class TourCatalog
{
    public static readonly Dictionary<string, List<TourStep>> Tours = new()
    {
        ["home"] =
        [
            new TourStep("global-search", "Search everything",
                "Type here and press Enter to search across all your notes and the public feed — not just what's on this page."),
            new TourStep("drawer-create-note", "Create a note",
                "Upload your documents here and Synapse turns them into a structured, AI-generated note — brain map, pages, and all."),
            new TourStep("home-feed", "Your feed",
                "Notes you and others have shared show up here — click a note's preview to open it, or its Brain Map, Process, or Map directly."),
            new TourStep("drawer-learn-synapse", "Come back anytime",
                "You can replay this tour whenever you like from here."),
        ],

        ["allnotes"] =
        [
            new TourStep("allnotes-actions", "Quick actions",
                "Start a new note, jump to Favorites, or group notes together — your notes list sits right below, with its own search and sort."),
        ],

        ["favorites"] =
        [
            new TourStep("favorites-list", "Your favorites",
                "Notes you've favorited from the feed collect here so you can find them again quickly."),
        ],

        ["connections"] =
        [
            new TourStep("connections-search", "Find a connection",
                "Search who you follow and who follows you by name — your Following and Followers lists sit side by side below."),
        ],

        ["my-requests"] =
        [
            new TourStep("my-requests-new", "Ask the team something",
                "Submit a question or a role-change request here, then track its status below — Pending, Approved, or Rejected."),
        ],

        ["create-note"] =
        [
            new TourStep("upload-dropzone", "Start with your documents",
                "Upload files here — Synapse's AI turns them into a structured note with a Brain Map, a Process view for Q&A, and a Map."),
        ],

        ["profile"] =
        [
            new TourStep("profile-nav", "Your profile",
                "Edit your info, and switch between your posts, favorites, and notes from these tabs."),
        ],
    };
}

// Singleton (registered alongside AuthState/NotesStore) — lets any component (Drawer's "Learn
// Synapse." button) ask a specific screen's TourGuide to replay its tour. Two paths, since the
// target TourGuide may or may not already be mounted: ConsumeReplay is checked by TourGuide on
// its own init (covers navigating in from another page), while ReplayRequested additionally
// fires live for a TourGuide that's already on screen (covers clicking it while already there).
public class TourState
{
    private string? _pendingReplayTourId;

    public event Action<string>? ReplayRequested;

    public void RequestReplay(string tourId)
    {
        _pendingReplayTourId = tourId;
        ReplayRequested?.Invoke(tourId);
    }

    public bool ConsumeReplay(string tourId)
    {
        if (_pendingReplayTourId != tourId)
            return false;

        _pendingReplayTourId = null;
        return true;
    }
}
