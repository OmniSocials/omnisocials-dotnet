# OmniSocials .NET SDK

The official .NET client for the [OmniSocials API](https://docs.omnisocials.com). Schedule and publish posts to Instagram, Facebook, LinkedIn, YouTube, TikTok, X, Pinterest, Bluesky, Threads, Mastodon, and Google Business from one API.

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

Create an API key in the OmniSocials app under **Settings -> API Keys**. Keys look like `omsk_live_...` (or `omsk_test_...`).

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

### Chained threads (X, Bluesky, Mastodon, Threads)

Provide 2 to 25 `thread_parts` to publish a chained thread instead of a single tweet. Each part is capped at 280 characters and can carry its own media (`media_ids` or `media_urls`). The same `thread_parts` shape works for `Bluesky` (300 chars per part), `Mastodon` (500 chars per part) and `Threads` (Meta Threads: 2 to 25 parts, 500 characters per part, up to 10 media per part; parts after the first publish as replies to the previous part, and the Threads caption is taken from part 1).

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

Uploading a PDF rasterizes it into one image slide per page (max 20). The response carries `slides` and `media_ids` alongside `data` (the first slide). Pass ALL of `media_ids`, in order, to `Posts.CreateAsync` to post the deck as a carousel (a native swipeable document on LinkedIn, an image carousel elsewhere).

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

```csharp
var best = await client.Analytics.BestTimesAsync(new BestTimesParams
{
    Platform = "instagram",
    Timezone = "Europe/Amsterdam",
});
```

## Locations (Instagram place tagging)

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

## Inbox (social inbox)

Conversations (DMs, comments, mentions) across connected platforms (Instagram,
Facebook, LinkedIn, TikTok comments, YouTube comments, and X DMs), their
message threads, and replies. TikTok and YouTube replies are comments only;
TikTok replies are capped at 150 characters. The list endpoints use **cursor
pagination**: page on by passing the previous response's
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
await client.Inbox.GetMessagesAsync(conversationId, new InboxMessageListParams { Limit = 20 });

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

Each API key carries scopes: `posts:read`, `posts:write`, `media:write`, `accounts:read`, `analytics:read`, `webhooks:manage`. A call with a missing scope throws `PermissionDeniedException` with code `insufficient_scope`.

## Documentation

Full API reference and guides: [https://docs.omnisocials.com](https://docs.omnisocials.com)

## License

MIT
