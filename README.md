# OmniSocials .NET SDK

The official .NET (C#) SDK for OmniSocials: post, schedule, reply and report on 11 social platforms (Instagram, Facebook, LinkedIn, X, TikTok, YouTube, Threads, Pinterest, Bluesky, Mastodon and Google Business) from one API. Guide: [omnisocials.com/sdk/dotnet](https://omnisocials.com/sdk/dotnet)

- Targets .NET 8, no dependencies beyond `System.Net.Http` and `System.Text.Json`
- Fully async API with `CancellationToken` support on every call
- Automatic retries with exponential backoff, configurable timeouts
- Rich exception types and a webhook signature verification helper

## Installation

```bash
dotnet add package OmniSocials
```

## Quickstart

```csharp
using OmniSocials;

var client = new OmniSocialsClient(); // reads OMNISOCIALS_API_KEY from env
var post = await client.Posts.CreateAsync(new PostCreateParams
{
    Content = "Hello from the SDK",
    Channels = new[] { "instagram", "linkedin" },
    ScheduledAt = "2026-08-01T09:00:00Z",
});
```

## Authentication

Create an API key in the OmniSocials app under **Settings -> API Keys**. Keys start with `omsk_live_`. There are no test keys and no test mode: every key works on your real connected accounts. To try things safely, leave out `ScheduledAt` so the post stays a draft (a draft never publishes on its own), or use a separate workspace with test accounts.

The client reads `OMNISOCIALS_API_KEY` from the environment, or you can pass it explicitly:

```csharp
var client = new OmniSocialsClient(new OmniSocialsOptions { ApiKey = "omsk_live_..." });
```

Constructing a client without a key throws an `AuthenticationException` right away.

## Configuration

```csharp
var client = new OmniSocialsClient(new OmniSocialsOptions
{
    ApiKey = "omsk_live_...",
    BaseUrl = "https://api.omnisocials.com/v1", // default
    Timeout = TimeSpan.FromSeconds(30),          // per-request timeout (default 30s)
    MaxRetries = 2,                              // automatic retries on 429 / 5xx / network errors (default 2)
});
```

Retries use exponential backoff (0.5s, 1s, 2s, ...) with jitter and honor the `Retry-After` header. Other 4xx responses are never retried. The timeout applies per attempt, so a retried request still gets its full time budget.

`OmniSocialsClient` owns an `HttpClient` internally and implements `IDisposable`; create one client and reuse it for the lifetime of your application.

## Rate limits

The API allows **100 requests per minute** per API key. When you exceed it, the SDK retries automatically (respecting `Retry-After`); if retries are exhausted it throws a `RateLimitException` whose `RetryAfter` property holds the seconds to wait.

## Return values

Methods return `Task<JsonElement?>` with the parsed response body as-is: single items come back as `{ "data": {...} }`, lists as `{ "data": [...], "pagination": {...} }`, and some responses carry extra sibling keys (media uploads include `compatibility`, PDF uploads include `slides` and `media_ids`, post creates targeting X with a URL in the text include `warnings`). Endpoints that respond `204 No Content` (deletes) resolve to `null`.

```csharp
var response = await client.Posts.ListAsync();
foreach (var post in response!.Value.GetProperty("data").EnumerateArray())
{
    Console.WriteLine($"{post.GetProperty("id")} {post.GetProperty("status")}");
}
```

## Posts

### Schedule a post

```csharp
var response = await client.Posts.CreateAsync(new PostCreateParams
{
    Content = "New drop this Friday",
    Channels = new[] { "instagram", "facebook", "linkedin" },
    ScheduledAt = "2026-08-01T09:00:00Z",
    MediaUrls = new[] { "https://example.com/teaser.jpg" },
});
var post = response!.Value.GetProperty("data");
Console.WriteLine($"{post.GetProperty("id")} {post.GetProperty("status")}");
```

Omit `ScheduledAt` to create a draft. Use a dictionary for per-platform captions:

```csharp
await client.Posts.CreateAsync(new PostCreateParams
{
    Content = new Dictionary<string, string>
    {
        ["default"] = "New drop this Friday",
        ["x"] = "New drop this Friday. RT to spread the word",
    },
    Channels = new[] { "instagram", "x" },
    ScheduledAt = "2026-08-01T09:00:00Z",
});
```

### Planned drafts

> **Not yet released (coming in 0.10.0).** This section describes the next SDK release. The current release is 0.9.0.

Set `Draft = true` with `ScheduledAt` to store the post as a draft that keeps its time. It shows on the dashboard calendar at that slot with a dashed outline and the scheduler never publishes it: a person schedules it from the composer, or you do with `UpdateAsync` and `Draft = false`. Use it when an agent proposes a slot that a human should confirm. A draft is not allowed with `publish_now` or `ApprovalWorkflowId` (`400 validation_error`). On `UpdateAsync`, `Draft = false` without `ScheduledAt` is a `400 validation_error` when the draft has no time or its time is in the past (pass a new `ScheduledAt` then), and `Draft = true` on a scheduled post unschedules it while keeping the time.

```csharp
var proposal = (await client.Posts.CreateAsync(new PostCreateParams
{
    Content = "Proposed for Friday morning",
    Channels = new[] { "instagram", "linkedin" },
    ScheduledAt = "2026-08-01T09:00:00Z",
    Draft = true,
}))!.Value.GetProperty("data");
Console.WriteLine($"{proposal.GetProperty("status")} {proposal.GetProperty("schedule_at")}"); // draft, the time is kept

// The human agrees: schedule it at the time it already carries
await client.Posts.UpdateAsync(proposal.GetProperty("id").GetString()!, new PostUpdateParams { Draft = false });
```

### Comment-to-DM automations

> **Not yet released (coming in 0.10.0).** This section describes the next SDK release. The current release is 0.9.0.

Answer a keyword in an Instagram comment with a DM, once per person, with an optional public reply under the comment. Set `CommentDmAutomation` on `CreateAsync` (Instagram must be in `Channels`) and the automation is bound to that post; it starts listening once the post is live. Needs the opt-in `automations:write` scope on the key. `client.Automations` manages the automations themselves (`ListAsync`, `GetAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`) and reads their activity (`ListEventsAsync`, `automations:read`). On `UpdateAsync`, `CommentDmAutomation` replaces the post's automation and `RemoveCommentDmAutomation = true` removes it (it sends the explicit `null` the null-omitting serializer would otherwise drop). Instagram rules: one private reply per comment, within 7 days; OmniSocials caps sends at 180 DMs an hour per account.

```csharp
var post = (await client.Posts.CreateAsync(new PostCreateParams
{
    Content = "Comment GUIDE and I will send you the link",
    Channels = new[] { "instagram" },
    ScheduledAt = "2026-08-01T09:00:00Z",
    MediaUrls = new[] { "https://example.com/guide-cover.jpg" },
    CommentDmAutomation = new Dictionary<string, object?>
    {
        ["trigger"] = new { keywords = new[] { "guide" }, match = "word" },
        ["actions"] = new
        {
            dm = new { text = "Here is the guide: https://example.com/guide" },
            public_reply = new { enabled = true, variations = new[] { "Sent you a DM!", "Check your inbox" } },
        },
        ["limits"] = new { max_sends = 500 },
    },
}))!.Value.GetProperty("data");
var automationId = post.GetProperty("comment_dm_automation").GetProperty("id").GetString()!;

// Later: who was answered, and why someone was skipped
var events = (await client.Automations.ListEventsAsync(automationId, new AutomationEventListParams { Outcome = "sent" }))!.Value;
Console.WriteLine(events.GetProperty("counts")); // {"sent":12,"skipped":3}
foreach (var item in events.GetProperty("data").EnumerateArray())
{
    Console.WriteLine($"{item.GetProperty("person").GetProperty("username")} {item.GetProperty("outcome")}");
}
```

The events response is `data`, `counts` and `pagination`, not the plain list: `counts` is the number of events per outcome (`sent`, `skipped`, `failed`, `pending`). An automation can also listen on a post published outside OmniSocials (`client.Automations.CreateAsync` with `InstagramMediaId`), on the next post the workspace publishes (`Trigger` with `["scope"] = "next_post"`) or on every post (`"any"`), and on DMs and story replies (`["kinds"] = new[] { "dm", "story_reply" }`).

### Publish immediately

```csharp
await client.Posts.CreateAndPublishAsync(new PostCreateParams
{
    Content = "Going live right now",
    Channels = new[] { "x", "bluesky" },
});
```

### Per-media alt text

Every `MediaUrls` / `MediaIds` entry accepts either a plain string or an object with an `alt` accessibility description (max 1500 chars): `new { url, alt }` for `MediaUrls`, `new { id, alt }` for `MediaIds`. Alt text is delivered to Mastodon (media description), Bluesky (embed alt), X (photos and GIFs), Pinterest (pin alt text), Instagram (images), and LinkedIn (images). Strings and objects can be mixed, and the same shape works in per-platform maps and `thread_parts` media.

```csharp
await client.Posts.CreateAsync(new PostCreateParams
{
    Content = "Sunrise over the harbor",
    Channels = new[] { "mastodon", "bluesky" },
    ScheduledAt = "2026-08-01T09:00:00Z",
    MediaUrls = new object[]
    {
        new
        {
            url = "https://example.com/harbor.jpg",
            alt = "A small sailboat crossing a calm harbor at sunrise, sky in deep orange",
        },
    },
});
```

### Post with platform-specific options

```csharp
await client.Posts.CreateAsync(new PostCreateParams
{
    Content = "Behind the scenes of our summer shoot",
    Channels = new[] { "instagram", "youtube", "x" },
    ScheduledAt = "2026-08-01T09:00:00Z",
    MediaUrls = new[] { "https://example.com/bts.mp4" },
    Instagram = new Dictionary<string, object?> { ["share_to_feed"] = true },
    Youtube = new Dictionary<string, object?> { ["title"] = "Summer shoot BTS", ["privacy"] = "public" },
    X = new Dictionary<string, object?> { ["reply_settings"] = "following", ["made_with_ai"] = false },
});
```

### Telegram channels and groups

> **Not yet released.** Telegram comes with the next SDK release.

`telegram` is a channel or group the workspace connected in OmniSocials; the OmniSocials bot posts to it. The `telegram` options: `silent`, `link_preview`, `pin`, `protect_content`, `spoiler` and `caption_above_media` (booleans), up to 3 `buttons` (`{ text, url }`, not on an album or a poll), a native `poll` (2 to 12 options of up to 100 characters, `open_period` 5 to 2628000 seconds, `multiple`; the post text is the question, max 300 characters, no media) and `first_comment` (a reply under the post). Text up to 4,096 characters; with media, text over 1,024 characters is sent as a reply under the media. Albums hold up to 10 photos and videos, files up to 50 MB, no Stories. Once sent, the post's `telegram.result` carries the message ids and the link. Telegram gives bots no view counts: analytics carry the reactions and the poll votes.

```csharp
await client.Posts.CreateAsync(new PostCreateParams
{
    Content = "The new collection is live. Tap the button to shop.",
    Channels = new[] { "telegram" },
    ScheduledAt = "2026-11-02T09:00:00Z",
    MediaUrls = new[] { "https://example.com/collection.jpg" },
    Telegram = new Dictionary<string, object?>
    {
        ["silent"] = true,
        ["pin"] = true,
        ["buttons"] = new[] { new Dictionary<string, object?> { ["text"] = "Shop now", ["url"] = "https://example.com/shop" } },
        ["first_comment"] = "Questions? Reply to this message.",
    },
});

// A native poll: the text is the question.
await client.Posts.CreateAsync(new PostCreateParams
{
    Content = "Which day suits the live Q&A?",
    Channels = new[] { "telegram" },
    Telegram = new Dictionary<string, object?>
    {
        ["poll"] = new Dictionary<string, object?>
        {
            ["options"] = new[] { "Tuesday", "Thursday", "Saturday" },
            ["open_period"] = 86400,
        },
    },
});
```

### Chained threads (X, Bluesky, Mastodon, Threads)

Provide 2 to 25 `thread_parts` to publish a chained thread instead of a single tweet. Each part is capped at 280 characters (25,000 for X Premium/Premium+ accounts) and can carry its own media (`media_ids` or `media_urls`). The same `thread_parts` shape works for `Bluesky` (300 chars per part), `Mastodon` (500 chars per part) and `Threads` (Meta Threads: 2 to 25 parts, 500 characters per part, up to 10 media per part; parts after the first publish as replies to the previous part, and the Threads caption is taken from part 1).

```csharp
await client.Posts.CreateAsync(new PostCreateParams
{
    Content = "How we grew to 10k followers in 90 days",
    Channels = new[] { "x" },
    ScheduledAt = "2026-08-01T09:00:00Z",
    X = new Dictionary<string, object?>
    {
        ["thread_parts"] = new[]
        {
            new Dictionary<string, object?> { ["text"] = "How we grew to 10k followers in 90 days. A thread:" },
            new Dictionary<string, object?> { ["text"] = "1. We posted every single day, even when it felt pointless." },
            new Dictionary<string, object?> { ["text"] = "2. We replied to every comment within an hour." },
            new Dictionary<string, object?> { ["text"] = "3. Full breakdown on our blog. Link in bio." },
        },
    },
});
```

```csharp
// Meta Threads chain with a carousel on the first part
await client.Posts.CreateAsync(new PostCreateParams
{
    Content = "Behind the scenes of our summer shoot",
    Channels = new[] { "threads" },
    Threads = new Dictionary<string, object?>
    {
        ["thread_parts"] = new[]
        {
            new Dictionary<string, object?>
            {
                ["text"] = "Behind the scenes of our summer shoot. A few highlights:",
                ["media_urls"] = new[] { "https://example.com/shoot-1.jpg", "https://example.com/shoot-2.jpg" },
            },
            new Dictionary<string, object?> { ["text"] = "Day one: scouting locations at sunrise." },
            new Dictionary<string, object?> { ["text"] = "Day two: the full crew, 14 hours, zero regrets." },
        },
    },
});
```

On update, set `["thread_parts"] = null` to clear thread mode (revert to a single post); omit the key to leave the existing thread untouched. Dictionary entries with explicit nulls are sent on the wire, while null POCO properties are omitted. The same applies to `Bluesky`, `Mastodon` and `Threads`.

The `Threads` options also accept `["location_id"]`: a Threads location id from `Locations.SearchAsync` with platform `"threads"` (see [Locations](#locations-instagram-and-threads-place-tagging)). On a multi-part thread the tag is applied to part 1, and on update `["location_id"] = null` clears the tag. Threads location tagging is currently rolling out; until Meta approves the permissions it is disabled on production and calls return a clear error.

### X link posts use credits

X bills API posts whose text contains a URL at a premium, and OmniSocials passes that fee through as prepaid credits (20 credits per URL-containing tweet; threads billed per part with a link). When a create targets X and the text contains a URL, the response carries a top-level `warnings` array (a sibling of `data`):

```csharp
var res = await client.Posts.CreateAsync(new PostCreateParams
{
    Content = "Read the full story: https://example.com/post",
    Channels = new[] { "x" },
});
if (res!.Value.TryGetProperty("warnings", out var warnings))
{
    foreach (var warning in warnings.EnumerateArray())
    {
        if (warning.GetProperty("code").GetString() == "x_url_post_credits")
        {
            Console.WriteLine(warning.GetProperty("credits_required").GetInt32());
        }
    }
}
```

From `enforce_from` (2026-08-14) the balance is checked at publish time, but credits are only deducted after the post successfully publishes (a failed publish is never charged). If the balance can't cover it, only the X target fails (other platforms publish normally); top up in the dashboard under Settings -> Organisation -> Billing -> Credits, then call `Posts.RetryAsync`. Posts without links, analytics, and media on X stay free. There is no API endpoint for credits — they are managed in the dashboard.

Separately, scheduling an X link post reserves its credit cost up front: `Posts.CreateAsync`, `UpdateAsync`, and `PublishAsync` now refuse before the request is even accepted if reserving that post's cost would push the company's total reserved credits past its balance. That throws a 402 `ApiException` (402 has no dedicated subclass, so check `Status`/`Code`) with `Code` `"x_credits_insufficient"` and `Body` carrying `error.details.credits_required` / `credits_balance` / `credits_reserved`:

```csharp
try
{
    await client.Posts.CreateAsync(new PostCreateParams
    {
        Content = "Read the full story: https://example.com/post",
        Channels = new[] { "x" },
        ScheduledAt = "2026-08-20T09:00:00Z",
    });
}
catch (ApiException ex) when (ex.Status == 402 && ex.Code == "x_credits_insufficient")
{
    var details = ex.Body!.Value.GetProperty("error").GetProperty("details");
    Console.WriteLine($"Need {details.GetProperty("credits_required")}, have {details.GetProperty("credits_balance")}");
}
```

Drafts are never gated, and posts scheduled to publish before 2026-08-14 are never gated. This is separate from the `x_url_post_credits` warning above (fires at create time, non-blocking) and from the publish-time-only-the-X-target-fails behavior described above.

### List, get, update, publish, retry, delete

```csharp
var listed = await client.Posts.ListAsync(new PostListParams { Status = "scheduled", Limit = 50 });
var first = listed!.Value.GetProperty("data")[0].GetProperty("id").GetString()!;

await client.Posts.GetAsync(first);
await client.Posts.UpdateAsync(first, new PostUpdateParams { ScheduledAt = "2026-08-02T10:00:00Z" });
await client.Posts.PublishAsync(first); // publish a draft/scheduled post now
await client.Posts.RetryAsync(first);   // retry only the failed platforms of a failed/warning post
await client.Posts.DeleteAsync(first);  // resolves to null (204)
```

`RetryAsync` re-publishes only the platforms that failed, on the same post; platforms that already succeeded are never posted again. It is asynchronous: a 200 means the retry is queued, so poll `GetAsync` for the outcome. Max 3 retries per platform.

### Approve or reject a post

```csharp
await client.Posts.ApproveAsync(id);                                       // approve the current approval-workflow step
await client.Posts.RejectAsync(id, "Wrong CTA link, please fix.");         // reject and stop the workflow (comment optional)
```

Only works on a post with `approval_status: "pending"` (`status: "in_approval"`). Both act on behalf of the user who owns the API key, who must be a listed approver for the workflow's CURRENT step — steps approve in order, so being an approver on a later step is not enough yet (throws a 403 `ApiException` with `Code` `forbidden`). Approving the last step finalizes the post (`scheduled` or `posting`); rejecting stops the whole workflow immediately, not just the current step.

### Read the approval review

```csharp
var review = (await client.Posts.GetApprovalAsync(id))!.Value.Deserialize<PostApprovalResponse>()!.Data;
if (review.Status == "rejected" && review.Rejection is not null)
{
    Console.WriteLine($"Rejected by {review.Rejection.By.Name}: {review.Rejection.Reason}");
}
foreach (var step in review.Steps)
{
    Console.WriteLine($"{step.Order} {step.Name} {step.Status}");
}
```

`GetApprovalAsync` returns the review of a post that went through an approval workflow: `status` (`none`, `pending`, `approved`, `rejected`), the `workflow`, who requested it and when, `current_step` (the step the post waits on, null when the review ended), every step with its approvers and their decisions, the `rejection` (`by`, `reason`, `at`, `step`; null when nobody rejected) and the `comments` thread, oldest first. Like every method it returns the raw `JsonElement?`; `PostApprovalResponse` is an optional typed model to deserialize it into. A post without an approval workflow returns `status: "none"` with empty `steps` and `comments`. Read-only; needs the `posts:read` scope.

### Recent platform posts

Fetch recent posts live from the connected platform APIs, including content published outside OmniSocials. Useful for brand-new workspaces where `ListAsync` is empty. Requires the `analytics:read` scope. Each record includes `duration_seconds` (integer, nullable): the video length in whole seconds where the platform reports it — currently TikTok and YouTube; `null` for images and for platforms that don't expose it.

```csharp
var recent = await client.Posts.RecentPlatformAsync(new RecentPlatformParams
{
    Limit = 10,
    Platforms = new[] { "instagram", "x" },
});
```

## Media

### Upload from a URL (recommended, up to 1GB)

```csharp
var upload = await client.Media.UploadFromUrlAsync(new MediaUploadFromUrlParams
{
    Url = "https://example.com/launch-video.mp4",
    Name = "launch-video-v2",
    Folder = "Campaigns",
});
Console.WriteLine(upload!.Value.GetProperty("data").GetProperty("id"));
```

Videos over 100MB are processed in the background and come back with status `"processing"`. Every upload response includes a `compatibility` block listing connected platforms that would reject the file.

### Upload a local file (multipart)

```csharp
// From a path
await client.Media.UploadAsync(MediaUploadParams.FromFile("./photos/product.jpg", name: "product-hero"));

// Or from bytes / a stream
var bytes = await File.ReadAllBytesAsync("./photos/product.jpg");
await client.Media.UploadAsync(MediaUploadParams.FromBytes(bytes, "product.jpg"));

await using var stream = File.OpenRead("./photos/product.jpg");
await client.Media.UploadAsync(MediaUploadParams.FromStream(stream, "product.jpg"));
```

Direct multipart uploads are capped at 100MB by the CDN; use `UploadFromUrlAsync` or the presigned flow below for bigger files.

### Upload from base64

```csharp
await client.Media.UploadFromBase64Async(new MediaUploadFromBase64Params
{
    Data = base64String, // no data URI prefix
    MimeType = "image/png",
    Filename = "chart.png",
});
```

### PDF carousels

Uploading a PDF rasterizes it into one image slide per page (max 20). The response carries `slides` and `media_ids` alongside `data` (the first slide). Pass ALL of `media_ids`, in order, to `Posts.CreateAsync` to post the deck as a carousel (on LinkedIn a native swipeable document made from the original PDF file, which is kept so text and links stay intact; an image carousel elsewhere). To keep the PDF as ONE library item instead of one item per page, pass `pdf_mode` = `"document"`: the response then has a single `data` item of type `document` (its page images in `pdf.pages`) and `media_ids` holds that one id, which expands into every page at post time.

```csharp
var pdf = await client.Media.UploadFromUrlAsync(new MediaUploadFromUrlParams
{
    Url = "https://example.com/deck.pdf",
});
var mediaIds = pdf!.Value.GetProperty("media_ids").EnumerateArray()
    .Select(id => id.GetString()!).ToArray();

await client.Posts.CreateAsync(new PostCreateParams
{
    Content = "Our Q3 strategy deck",
    Channels = new[] { "linkedin" },
    MediaIds = mediaIds,
    ScheduledAt = "2026-08-01T09:00:00Z",
});
```

### Presigned uploads for large files (up to 1GB)

`CreateUploadUrlAsync` mints a one-time upload URL. POST the file to it as multipart form data (field name `file`) within `expires_in_seconds` (600s); the second request needs no auth headers because the single-use token is in the URL. The response of that second request is the created media item (or `media_ids` for a PDF).

```csharp
var minted = await client.Media.CreateUploadUrlAsync();
var uploadUrl = minted!.Value.GetProperty("upload_url").GetString()!;

using var http = new HttpClient();
using var form = new MultipartFormDataContent();
form.Add(new ByteArrayContent(await File.ReadAllBytesAsync("./big-video.mp4")), "file", "big-video.mp4");
var uploaded = await http.PostAsync(uploadUrl, form);
Console.WriteLine(await uploaded.Content.ReadAsStringAsync());
```

### Preflight compatibility check

Check a file against the workspace's connected platforms before uploading. Provide one of `Url`, `MediaId`, or `SizeBytes` + `Mime`.

```csharp
await client.Media.CheckAsync(new MediaCheckParams { Url = "https://example.com/huge.mov" });
await client.Media.CheckAsync(new MediaCheckParams { SizeBytes = 300_000_000, Mime = "video/quicktime" });
```

### List, get, rename, move, delete

```csharp
var items = await client.Media.ListAsync(new MediaListParams { Search = "hero", Limit = 20 });
var id = items!.Value.GetProperty("data")[0].GetProperty("id").GetString()!;

await client.Media.UpdateAsync(id, new MediaUpdateParams { Name = "hero-v2", FolderId = "12" });
await client.Media.UpdateAsync(id, new MediaUpdateParams { MoveToRoot = true }); // back to "All media"
await client.Media.GetAsync(id);
await client.Media.DeleteAsync(id); // 409 media_in_use if attached to a scheduled post
```

## Folders

```csharp
var folders = await client.Folders.ListAsync(); // flat; build the tree via parent_id
var created = await client.Folders.CreateAsync(new FolderCreateParams { Name = "Campaigns" });
var folderId = created!.Value.GetProperty("data").GetProperty("id").GetString()!;

await client.Folders.UpdateAsync(folderId, new FolderUpdateParams { Name = "Campaigns 2026" });
await client.Folders.UpdateAsync(folderId, new FolderUpdateParams { MoveToTopLevel = true });
await client.Folders.DeleteAsync(folderId); // files move to root, subfolders move up
```

## Hashtag Sets

Save reusable hashtag groups and apply them to posts at create time. Uses the `posts:read` / `posts:write` scopes.

```csharp
var created = await client.HashtagSets.CreateAsync(new HashtagSetCreateParams
{
    Name = "Launch",
    Hashtags = new[] { "saas", "buildinpublic", "startup" }, // or one string: "#saas #buildinpublic #startup"
});
var setId = created!.Value.GetProperty("data").GetProperty("id").GetString()!;
Console.WriteLine(created.Value.GetProperty("data").GetProperty("preview").GetString()); // "#saas #buildinpublic #startup"

await client.HashtagSets.ListAsync();
await client.HashtagSets.GetAsync(setId);
await client.HashtagSets.UpdateAsync(setId, new HashtagSetUpdateParams
{
    Hashtags = new[] { "saas", "founder" }, // replaces the full list
});
await client.HashtagSets.DeleteAsync(setId); // resolves to null (204)
```

Apply a set when creating a post with `HashtagSet` (the set name, case-insensitive) or `HashtagSetId`. The set is applied once at create time and tags already in the caption are skipped. `HashtagPlacement` is `"caption_append"` (default) or `"first_comment"`, and `HashtagPlatforms` restricts the hashtags to a subset of the post's channels. Instagram's 30-hashtag cap returns error code `hashtag_limit_exceeded`.

```csharp
await client.Posts.CreateAsync(new PostCreateParams
{
    Content = "Launch day!",
    Channels = new[] { "instagram", "x" },
    ScheduledAt = "2026-08-01T09:00:00Z",
    HashtagSet = "Launch",
    HashtagPlacement = "first_comment",
    HashtagPlatforms = new[] { "instagram" },
});
```

## Accounts

```csharp
var accounts = await client.Accounts.ListAsync();
foreach (var account in accounts!.Value.GetProperty("data").EnumerateArray())
{
    Console.WriteLine($"{account.GetProperty("platform")} {account.GetProperty("username")} {account.GetProperty("status")}");
    if (account.GetProperty("needs_reconnect").GetBoolean())
    {
        Console.WriteLine($"  needs a reconnect: {account.GetProperty("reauth_reason")}");
    }
}
```

## Analytics

```csharp
// One post's latest per-platform metrics
var stats = await client.Analytics.PostAsync("post_id");
Console.WriteLine(stats!.Value.GetProperty("data").GetProperty("platforms"));

// Batch: up to 100 posts in one call
var batch = await client.Analytics.PostsAsync(new[] { "id1", "id2", "id3" });

// Workspace-wide overview
var overview = await client.Analytics.OverviewAsync(new AnalyticsOverviewParams { Period = "30d" });
var totals = overview!.Value.GetProperty("data");
Console.WriteLine($"{totals.GetProperty("total_impressions")} impressions");

// Account-level stats (followers etc)
var accountStats = await client.Analytics.AccountsAsync(new AccountAnalyticsParams { Platform = "instagram" });
```

### Best times to post

The response is not wrapped in `data`. `platform`, `timezone`, `basis`, `grid` and `recommendations` sit at the top level.

```csharp
var best = await client.Analytics.BestTimesAsync(new BestTimesParams
{
    Platform = "instagram",
    Timezone = "Europe/Amsterdam",
});
foreach (var slot in best!.Value.GetProperty("recommendations").EnumerateArray())
{
    Console.WriteLine($"{slot.GetProperty("day")} {slot.GetProperty("time")}");
}
```

## Locations (Instagram and Threads place tagging)

```csharp
var results = await client.Locations.SearchAsync("Griffith Observatory");
var place = results!.Value.GetProperty("data")[0];
var placeId = place.GetProperty("id").GetString()!;

var check = await client.Locations.ValidateAsync(placeId);
if (check!.Value.GetProperty("valid").GetBoolean())
{
    await client.Posts.CreateAsync(new PostCreateParams
    {
        Content = "Golden hour at the observatory",
        Channels = new[] { "instagram" },
        MediaUrls = new[] { "https://example.com/observatory.jpg" },
        LocationId = placeId,
        ScheduledAt = "2026-08-01T18:30:00Z",
    });
}
```

Pass a `LocationSearchParams` with `Platform = "threads"` to search Meta's Threads location catalog instead of Facebook Places (the two sources use different ids). With `Platform = "threads"` you can also search around a point with `Latitude` + `Longitude` instead of `Q` (pass either `Q` or the coordinate pair). The Threads response is `{ "locations": [...] }` (each entry: `id`, plus nullable `name`, `address`, `city`, `country`, `latitude`, `longitude`) or `{ "error": { "code", "message" } }` with code `not_available`, `threads_not_connected`, `threads_reauth_required` (reconnect Threads), or `platform_error`. Use a result's `id` as `["location_id"]` inside the `Threads` post options, not as the top-level `LocationId`. Threads location tagging is currently rolling out; until Meta approves the permissions it is disabled on production and calls return a clear error.

```csharp
var threadsPlaces = await client.Locations.SearchAsync(new LocationSearchParams
{
    Q = "Griffith Observatory",
    Platform = "threads",
});
// Or search around a point instead of by text:
var nearby = await client.Locations.SearchAsync(new LocationSearchParams
{
    Platform = "threads",
    Latitude = 34.1184,
    Longitude = -118.3004,
});
var threadsLocationId = threadsPlaces!.Value.GetProperty("locations")[0].GetProperty("id").GetString()!;

await client.Posts.CreateAsync(new PostCreateParams
{
    Content = "Golden hour at the observatory",
    Channels = new[] { "threads" },
    MediaUrls = new[] { "https://example.com/observatory.jpg" },
    Threads = new Dictionary<string, object?> { ["location_id"] = threadsLocationId },
    ScheduledAt = "2026-08-01T18:30:00Z",
});
```

## Pinterest product tags

Tag products on a Pin so people can shop the items in the image. `client.Pinterest.ListProductsAsync()` returns the product Pins of the connected Pinterest account; pass their `pin_id` values (max 24) as `["product_tags"]` in the `Pinterest` options of the post. Only product Pins of your own account can be tagged; products of other merchants cannot. The tags are added right after the Pin is published. A product that Pinterest refuses never fails the post: the outcome is on the post as `pinterest.product_tags_result` (`requested`, `tagged`, `skipped`, `error`).

```csharp
var result = (await client.Pinterest.ListProductsAsync())!.Value;

if (result.TryGetProperty("error", out var error))
{
    // HTTP 200 without "products": pinterest_not_connected,
    // pinterest_catalog_access_required or platform_error
    Console.Error.WriteLine($"{error.GetProperty("code").GetString()}: {error.GetProperty("message").GetString()}");
}
else
{
    var productTags = result.GetProperty("products").EnumerateArray()
        .Take(3)
        .Select(product => product.GetProperty("pin_id").GetString()!)
        .ToArray();

    await client.Posts.CreateAsync(new PostCreateParams
    {
        Content = "Our summer picks",
        Channels = new[] { "pinterest" },
        MediaUrls = new[] { "https://example.com/summer-look.jpg" },
        ScheduledAt = "2026-08-01T09:00:00Z",
        Pinterest = new Dictionary<string, object?>
        {
            ["board_id"] = "1234567890",
            ["title"] = "Summer picks",
            ["product_tags"] = productTags,
        },
    });
}
```

Without `Source` the list reads the Pinterest catalog (with `price`, `currency`, `availability` and `item_id`) when the connection has catalog access, else the account's own Pins. Catalog access is given one time in the OmniSocials composer: Pinterest options, Add products, Connect catalog. `Source = "pins"` scans up to 250 Pins per call, so `products` can be empty while `bookmark` is set; call again with `new PinterestProductListParams { Bookmark = bookmark }`. To check one Pin id or Pin link before you post, call `client.Pinterest.ValidateProductAsync("813744226420795884")`. On `Posts.UpdateAsync` the `Pinterest` dictionary replaces the stored one, so leave `["product_tags"]` out to remove the tags.

## Inbox (social inbox)

Conversations (DMs, comments, mentions) across connected platforms (Instagram,
Facebook, LinkedIn, TikTok comments, YouTube comments, X DMs, and Threads),
their message threads, and replies. TikTok and YouTube replies are comments
only; TikTok replies are capped at 150 characters. Threads conversations are
`type` `"comment"` (replies people leave on your Threads posts; conversation
ids look like `threads_comment_<rootPostId>`) and `"mention"`
(`threads_mention_<postId>`); there are no Threads DMs, and a reply publishes
as a native Threads reply. The Threads inbox needs a Threads connection with
the reply permission (a
connection without it throws a 401 `AuthenticationException` with code
`reauth_required`; reconnect Threads to fix it). The list endpoints use
**cursor pagination**: page on by passing the previous response's
`pagination.next_cursor` back as `Cursor` while `pagination.has_more` is true.

```csharp
var page = await client.Inbox.ListConversationsAsync(new InboxConversationListParams
{
    Platform = "instagram",
    Type = "comment",
    Unread = true,
    Limit = 50,
});

var conversation = page!.Value.GetProperty("data")[0];
var conversationId = conversation.GetProperty("conversation_id").GetString()!;

// Full thread for one conversation (newest first). The id is URL-encoded for
// you, so pass it exactly as returned - LinkedIn ids contain ":" and "()".
var messages = await client.Inbox.GetMessagesAsync(conversationId, new InboxMessageListParams { Limit = 20 });
var messageId = messages!.Value.GetProperty("data")[0].GetProperty("id").GetString()!;

// Mark every message in the conversation as read.
var read = await client.Inbox.MarkReadAsync(conversationId);
Console.WriteLine(read!.Value.GetProperty("marked_read").GetInt32());

// Reply into the conversation, optionally attaching one media asset by URL.
await client.Inbox.ReplyAsync(conversationId, new InboxReplyParams
{
    Text = "Thanks for reaching out!",
    AttachmentUrl = "https://example.com/reply.jpg",
    AttachmentType = "image",
});

// Hide (or unhide) a comment someone left on one of your posts, as the post
// owner: Facebook, Instagram, TikTok, YouTube and Threads comments (Threads:
// incoming top-level replies only). The message keeps its place in the
// conversation and comes back with `hidden` flipped.
await client.Inbox.HideAsync(messageId);              // hide
await client.Inbox.HideAsync(messageId, hide: false); // unhide

// Delete a comment outright (Facebook, Instagram, TikTok; YouTube: hide
// instead). Replies under it go with it; their ids come back as
// removed_reply_ids.
await client.Inbox.DeleteMessageAsync(messageId);
```

**Work queue: what needs an answer.** `NextAsync` hands out the next conversation that still needs a reply (the customer's latest DM with no reply after it, or an unreplied comment/mention that is not hidden), with the whole thread and the post it belongs to (`post.url`, `post.media_type`), so a reply can be drafted from one call. DMs that can still be answered come first, then Instagram/Facebook DMs whose 24-hour window has closed (`reply_window.open` is false: answer those from the native app or mark them read), then comments and mentions, oldest first. Replies typed in the native apps count as answers. Only unread items are served by default, so `MarkReadAsync` is the durable way to skip one; `Exclude` skips conversation ids for the current session only. Always set `MessageId` to the served `message.id` on `ReplyAsync` for comment threads: every comment on a post shares one conversation, and without it the reply goes under the newest comment on the post. Set `IncludeNext` on `ReplyAsync` to get the following item in the same response. `InboxConversationListParams { Unanswered = true }` gives the same set as a plain list. Typed models: `InboxNextResponse` / `InboxNextUnanswered` / `InboxReplyWindow`, `InboxDeleteMessageResponse`.

```csharp
var next = await client.Inbox.NextAsync(new InboxNextParams { Platform = "instagram" });
var item = next!.Value.GetProperty("data");
while (item.ValueKind != JsonValueKind.Null)
{
    var message = item.GetProperty("message");
    Console.WriteLine($"{message.GetProperty("sender").GetProperty("username").GetString()}: {message.GetProperty("text").GetString()}");

    if (!item.GetProperty("reply_window").GetProperty("open").GetBoolean())
    {
        // An Instagram/Facebook DM past Meta's 24-hour window: ReplyAsync would
        // throw 422 outside_messaging_window. Answer it in the app, or skip it.
        await client.Inbox.MarkReadAsync(message.GetProperty("conversation_id").GetString()!);
        next = await client.Inbox.NextAsync(new InboxNextParams { Platform = "instagram" });
        item = next!.Value.GetProperty("data");
        continue;
    }

    var reply = await client.Inbox.ReplyAsync(message.GetProperty("conversation_id").GetString()!, new InboxReplyParams
    {
        Text = "Thanks! DM sent.",
        MessageId = message.GetProperty("id").GetString(), // the comment being answered, not the newest one
        IncludeNext = true,
    });
    item = reply!.Value.GetProperty("next"); // JSON null when nothing else is waiting; "remaining" sits beside it
}
```

X DM replies cost 2 prepaid credits per send, debited from the company balance before the send and auto-refunded if the send fails. `ReplyAsync` can throw a 402 `ApiException` (402 has no dedicated subclass, so check `Status`/`Code`) with one of two codes: `"insufficient_credits"` (the balance can't cover the 2 credits) or `"x_inbox_suspended"` (the workspace's X inbox auto-suspended when the balance hit zero - top up and re-enable it in the dashboard to resume; DMs that arrive while suspended are not recovered):

```csharp
try
{
    await client.Inbox.ReplyAsync(conversationId, new InboxReplyParams { Text = "On it!" });
}
catch (ApiException ex) when (ex.Status == 402 && ex.Code == "x_inbox_suspended")
{
    Console.WriteLine("X inbox is suspended - top up credits and re-enable it in the dashboard.");
}
catch (ApiException ex) when (ex.Status == 402 && ex.Code == "insufficient_credits")
{
    Console.WriteLine("Not enough credits for this reply.");
}
```

Like every resource, these methods return the raw `JsonElement?`. Optional typed
models mirror the payloads if you prefer strong typing:

```csharp
var typed = page!.Value.Deserialize<InboxConversationsResponse>();
foreach (var c in typed!.Data)
{
    Console.WriteLine($"{c.Platform} {c.Type}: {c.UnreadCount} unread from {c.Participant.Username}");
}
if (typed.Pagination.HasMore)
{
    await client.Inbox.ListConversationsAsync(new InboxConversationListParams { Cursor = typed.Pagination.NextCursor });
}
```

## Webhooks

Events: `post.scheduled`, `post.published`, `post.failed`, `post.approved` (the last step of a post's approval workflow is approved) and `post.rejected` (an approver rejects the post; it will not publish). The two approval events carry `data.approval` with `status`, `decided_by` (the approver's user id) and `reason` (null on `post.approved`), and an empty `data.targets`.

### Manage endpoints

```csharp
var created = await client.Webhooks.CreateAsync(new WebhookCreateParams
{
    Url = "https://example.com/omnisocials/webhook",
    Events = new List<string> { "post.published", "post.failed" },
});
var webhook = created!.Value.GetProperty("data");
Console.WriteLine(webhook.GetProperty("secret")); // save it, it is only shown once
var webhookId = webhook.GetProperty("id").GetString()!;

await client.Webhooks.ListAsync();
await client.Webhooks.GetAsync(webhookId);
await client.Webhooks.UpdateAsync(webhookId, new WebhookUpdateParams { IsActive = false });
var rotated = await client.Webhooks.RotateSecretAsync(webhookId);
Console.WriteLine(rotated!.Value.GetProperty("data").GetProperty("secret")); // the old secret stops working
await client.Webhooks.DeleteAsync(webhookId);
```

### Verify deliveries (ASP.NET Core minimal API example)

Every delivery is signed with your webhook secret. The `X-OmniSocials-Signature` header has the form `t=<unix>,v1=<hex>` where the hex value is an HMAC-SHA256 of `"{timestamp}.{rawBody}"`. Always verify against the RAW request body:

```csharp
using System.Text.Json;
using OmniSocials;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/omnisocials/webhook", async (HttpRequest request) =>
{
    using var reader = new StreamReader(request.Body);
    var payload = await reader.ReadToEndAsync(); // raw body, do not re-serialize
    var signature = request.Headers["X-OmniSocials-Signature"].ToString();
    var secret = Environment.GetEnvironmentVariable("OMNISOCIALS_WEBHOOK_SECRET")!;

    JsonElement webhookEvent;
    try
    {
        webhookEvent = WebhookSignature.Verify(payload, signature, secret, toleranceSeconds: 300);
    }
    catch (WebhookVerificationException)
    {
        return Results.BadRequest();
    }

    switch (webhookEvent.GetProperty("type").GetString())
    {
        case "post.published":
            var data = webhookEvent.GetProperty("data");
            Console.WriteLine($"Published: {data.GetProperty("post_id")} {data.GetProperty("targets")}");
            break;
        case "post.failed":
            Console.Error.WriteLine($"Failed: {webhookEvent.GetProperty("data").GetProperty("post_id")}");
            break;
        case "post.rejected":
            var rejected = webhookEvent.GetProperty("data");
            Console.Error.WriteLine($"Rejected: {rejected.GetProperty("post_id")} {rejected.GetProperty("approval").GetProperty("reason")}");
            break;
    }
    return Results.Ok();
});

app.Run();
```

`WebhookSignature.Verify` uses a constant-time comparison (`CryptographicOperations.FixedTimeEquals`), rejects timestamps older than `toleranceSeconds` (replay protection), throws `WebhookVerificationException` on any failure, and returns the parsed event as a `JsonElement` on success.

## Health

```csharp
var health = await client.HealthAsync(); // { "status": "ok", "version": "1.0.0", "timestamp": "..." }
```

## Error handling

All exceptions thrown by the SDK extend `OmniSocialsException`. Non-2xx API responses throw an `ApiException` subclass with `Status`, `Code`, `Message`, and the parsed `Body`:

| Exception | Status | Typical API codes |
|---|---|---|
| `ValidationException` | 400 / 422 | `validation_error`, `platform_not_connected`, `invalid_file_type` |
| `AuthenticationException` | 401 | `unauthorized`, `invalid_api_key` |
| `PermissionDeniedException` | 403 | `forbidden`, `insufficient_scope` |
| `NotFoundException` | 404 | `not_found` |
| `ApiException` (no dedicated subclass) | 402 | `x_credits_insufficient`, `insufficient_credits`, `x_inbox_suspended` |
| `RateLimitException` | 429 | `rate_limit_exceeded` (exposes `RetryAfter` seconds) |
| `ServerException` | >= 500 | `internal_error` |
| `ApiConnectionException` | n/a | network failure or timeout |
| `WebhookVerificationException` | n/a | invalid webhook signature |

```csharp
try
{
    await client.Posts.CreateAsync(new PostCreateParams
    {
        Content = "Hi",
        Channels = new[] { "instagram" },
    });
}
catch (RateLimitException ex)
{
    Console.WriteLine($"Rate limited, retry in {ex.RetryAfter}s");
}
catch (ValidationException ex)
{
    Console.WriteLine($"Bad request ({ex.Code}): {ex.Message}");
}
catch (ApiConnectionException ex)
{
    Console.WriteLine($"Network problem: {ex.Message}");
}
catch (ApiException ex)
{
    Console.WriteLine($"API error {ex.Status} ({ex.Code}): {ex.Message}");
}
```

## API scopes

Each API key carries scopes: `posts:read`, `posts:write`, `media:write`, `accounts:read`, `analytics:read`, `webhooks:manage`, plus the opt-in `automations:read` / `automations:write` for comment-to-DM automations (not yet released in the SDK, coming in 0.10.0). A call with a missing scope throws `PermissionDeniedException` with code `insufficient_scope`.

## Documentation

Full API reference and guides: [https://docs.omnisocials.com](https://docs.omnisocials.com)

## License

MIT
