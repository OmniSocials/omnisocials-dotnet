using System.Text.Json.Serialization;

namespace OmniSocials;

// Typed response models for GET /posts/:id/approval. Like every resource
// method, PostsResource.GetApprovalAsync returns the raw JsonElement? so
// callers can read the payload directly. These classes are optional,
// strongly-typed deserialization targets: e.g.
//   (await client.Posts.GetApprovalAsync(id))?.Deserialize<PostApprovalResponse>().
// They mirror the API's snake_case fields via [JsonPropertyName].

/// <summary>A person named in a post's approval review.</summary>
public sealed class PostApprovalUser
{
    /// <summary>The user id.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

/// <summary>The workflow a post's review runs on.</summary>
public sealed class PostApprovalWorkflow
{
    /// <summary>Null for a one-off approval that was not made from a saved workflow.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
}

/// <summary>One approver on a step of a post's approval review.</summary>
public sealed class PostApprovalApprover
{
    /// <summary>The approver's user id.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>"pending", "approved", or "rejected".</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    /// <summary>When this approver decided (ISO 8601); null while pending.</summary>
    [JsonPropertyName("decided_at")]
    public string? DecidedAt { get; set; }

    /// <summary>The reason this approver gave with a rejection; null otherwise.</summary>
    [JsonPropertyName("comment")]
    public string? Comment { get; set; }
}

/// <summary>One step of a post's approval review.</summary>
public sealed class PostApprovalStep
{
    /// <summary>1-based step order; steps are approved in order.</summary>
    [JsonPropertyName("order")]
    public int Order { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    /// <summary>"any" (one approver of the step is enough) or "all" (every approver must approve).</summary>
    [JsonPropertyName("require_mode")]
    public string RequireMode { get; set; } = "";

    /// <summary>"pending", "approved", or "rejected".</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("approvers")]
    public IList<PostApprovalApprover> Approvers { get; set; } = new List<PostApprovalApprover>();
}

/// <summary>Who rejected the post, why, when, and on which step.</summary>
public sealed class PostApprovalRejection
{
    [JsonPropertyName("by")]
    public PostApprovalUser By { get; set; } = new();

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    /// <summary>When the post was rejected (ISO 8601).</summary>
    [JsonPropertyName("at")]
    public string? At { get; set; }

    /// <summary>Order of the step the rejection happened on.</summary>
    [JsonPropertyName("step")]
    public int? Step { get; set; }
}

/// <summary>One entry of the review thread.</summary>
public sealed class PostApprovalComment
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    /// <summary>Null when the author is not known.</summary>
    [JsonPropertyName("author")]
    public PostApprovalUser? Author { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = "";

    /// <summary>The channel the comment is about ("instagram", "linkedin_page", ...); null = the whole post.</summary>
    [JsonPropertyName("account")]
    public string? Account { get; set; }

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = "";
}

/// <summary>The approval review of a post (the <c>data</c> of <c>GET /posts/:id/approval</c>).</summary>
public sealed class PostApproval
{
    [JsonPropertyName("post_id")]
    public string PostId { get; set; } = "";

    /// <summary>
    /// "none", "pending", "approved", or "rejected". "none" = the post has no
    /// approval workflow: <see cref="Workflow"/>, <see cref="RequestedBy"/>,
    /// <see cref="RequestedAt"/>, <see cref="CurrentStep"/> and
    /// <see cref="Rejection"/> are null, and <see cref="Steps"/> and
    /// <see cref="Comments"/> are empty.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("workflow")]
    public PostApprovalWorkflow? Workflow { get; set; }

    [JsonPropertyName("requested_by")]
    public PostApprovalUser? RequestedBy { get; set; }

    [JsonPropertyName("requested_at")]
    public string? RequestedAt { get; set; }

    /// <summary>Order of the step the post waits on; null when the review ended.</summary>
    [JsonPropertyName("current_step")]
    public int? CurrentStep { get; set; }

    [JsonPropertyName("steps")]
    public IList<PostApprovalStep> Steps { get; set; } = new List<PostApprovalStep>();

    /// <summary>Set when an approver rejected the post; null otherwise.</summary>
    [JsonPropertyName("rejection")]
    public PostApprovalRejection? Rejection { get; set; }

    /// <summary>
    /// The review thread, oldest first. Includes the entries OmniSocials
    /// writes when a reviewer edits the post.
    /// </summary>
    [JsonPropertyName("comments")]
    public IList<PostApprovalComment> Comments { get; set; } = new List<PostApprovalComment>();
}

/// <summary>Envelope for <c>GET /posts/:id/approval</c>.</summary>
public sealed class PostApprovalResponse
{
    [JsonPropertyName("data")]
    public PostApproval Data { get; set; } = new();
}
