using System.Text.Json;
using System.Text.Json.Nodes;

namespace OmniSocials;

/// <summary>Posts: create, schedule, publish, update, delete.</summary>
public sealed class PostsResource
{
    private readonly OmniSocialsClient _client;

    internal PostsResource(OmniSocialsClient client) => _client = client;

    /// <summary><c>GET /posts</c>: list posts in the workspace (newest first).</summary>
    public Task<JsonElement?> ListAsync(PostListParams? parameters = null, CancellationToken cancellationToken = default)
    {
        var query = new List<KeyValuePair<string, string?>>
        {
            new("status", parameters?.Status),
            new("limit", parameters?.Limit?.ToString()),
            new("offset", parameters?.Offset?.ToString()),
            new("start_date", parameters?.StartDate),
            new("end_date", parameters?.EndDate),
            new("tag", parameters?.Tag),
        };
        return _client.GetAsync("/posts", query, cancellationToken);
    }

    /// <summary><c>GET /posts/:id</c>: fetch a single post.</summary>
    public Task<JsonElement?> GetAsync(string id, CancellationToken cancellationToken = default)
        => _client.GetAsync($"/posts/{Uri.EscapeDataString(id)}", null, cancellationToken);

    /// <summary>
    /// <c>GET /posts/recent-platform</c>: recent posts fetched live from the
    /// connected platform APIs (including content published outside OmniSocials).
    /// The fallback for brand-new workspaces where ListAsync is empty. Requires
    /// the analytics:read scope.
    /// </summary>
    public Task<JsonElement?> RecentPlatformAsync(RecentPlatformParams? parameters = null, CancellationToken cancellationToken = default)
    {
        var query = new List<KeyValuePair<string, string?>>
        {
            new("limit", parameters?.Limit?.ToString()),
            new("platforms", parameters?.Platforms is { Count: > 0 } platforms ? string.Join(",", platforms) : null),
        };
        return _client.GetAsync("/posts/recent-platform", query, cancellationToken);
    }

    /// <summary>
    /// <c>POST /posts/create</c>: create a draft or scheduled post. When the
    /// post targets X and its text (or any thread part) contains a URL, the
    /// response includes a top-level <c>warnings</c> array (sibling of
    /// <c>data</c>) with a <c>x_url_post_credits</c> entry carrying
    /// <c>credits_required</c> and <c>credits_balance</c>: X's link-post fee
    /// is passed through as prepaid credits, debited at publish time (from
    /// 2026-08-14). Credits are managed in the dashboard, not the API.
    ///
    /// Separately, scheduling an X link post - here, or via
    /// <see cref="UpdateAsync"/> or <see cref="PublishAsync"/> - reserves that
    /// post's credit cost up front: if reserving it would push the company's
    /// total reserved credits past its balance, the call refuses before the
    /// request is even accepted, throwing a 402 <see cref="ApiException"/>
    /// (402 isn't one of the typed subclasses, so check <c>Status</c>/<c>Code</c>)
    /// with <c>Code</c> <c>x_credits_insufficient</c> and <c>Body</c> carrying
    /// <c>error.details</c> with <c>credits_required</c>, <c>credits_balance</c>,
    /// and <c>credits_reserved</c>. Drafts are never gated, and posts scheduled
    /// to publish before 2026-08-14 are never gated. This is distinct from the
    /// <c>x_url_post_credits</c> warning above (create-time, non-blocking) and
    /// from the publish-time-only-the-X-target-fails behavior once enforcement
    /// starts.
    ///
    /// The same <c>warnings</c> array carries one <c>image_fit</c> entry per
    /// channel decision on a feed image whose ratio is outside what the channel
    /// shows uncropped (<c>action</c> is <c>cropped</c>, <c>kept</c> or
    /// <c>crop_failed</c>, with <c>platform</c>, <c>media_url</c>, <c>ratio</c>,
    /// <c>target</c> and <c>crop_url</c>); <see cref="PostCreateParams.MediaFit"/>
    /// steers it per channel.
    ///
    /// <see cref="PostCreateParams.CommentDmAutomation"/> binds a comment-to-DM
    /// automation to the post (Instagram must be in the channels; needs the
    /// <c>automations:write</c> scope on the key). The response then carries
    /// <c>data.comment_dm_automation</c>, and the automation starts listening
    /// once the post is live on Instagram.
    ///
    /// A media id that is still processing (a large video is checked and
    /// converted after upload) answers <c>409 media_processing</c> and nothing
    /// is created: wait until <c>Media.GetAsync</c> says <c>ready</c>, then
    /// send the same request again. A file whose processing failed answers
    /// <c>400 invalid_media</c>: upload it again.
    /// </summary>
    public Task<JsonElement?> CreateAsync(PostCreateParams parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync("/posts/create", parameters, cancellationToken);

    /// <summary>
    /// <c>POST /posts/create-and-publish</c>: create a post and publish it immediately.
    /// See <see cref="CreateAsync"/> for the <c>warnings</c> array on X link
    /// posts and the <c>409 media_processing</c> / <c>400 invalid_media</c>
    /// answers.
    /// </summary>
    public Task<JsonElement?> CreateAndPublishAsync(PostCreateParams parameters, CancellationToken cancellationToken = default)
        => _client.PostAsync("/posts/create-and-publish", parameters, cancellationToken);

    /// <summary>
    /// <c>PATCH /posts/:id</c>: update a draft or scheduled post. Like
    /// <see cref="CreateAsync"/>, the response can carry a top-level
    /// <c>warnings</c> array: <c>image_fit</c> entries when the update changes
    /// the images or <c>media_fit</c> (<see cref="PostUpdateParams.MediaFit"/>
    /// alone re-decides the stored images; <c>keep</c> gives the original back).
    /// <see cref="PostUpdateParams.CommentDmAutomation"/> replaces the post's
    /// comment-to-DM automation and
    /// <see cref="PostUpdateParams.RemoveCommentDmAutomation"/> removes it
    /// (needs the <c>automations:write</c> scope); omit both to leave it.
    /// <see cref="PostUpdateParams.Discord"/> merges into the stored Discord
    /// options and <see cref="PostUpdateParams.RemoveDiscord"/> removes them all.
    ///
    /// 409 answers, where nothing is changed: <c>post_publishing</c> while the
    /// post is publishing (status <c>posting</c>) or a retry publishes one of
    /// its channels (poll <see cref="GetAsync"/> until <c>published</c>,
    /// <c>warning</c> or <c>failed</c>; an update with only <c>tags</c> still
    /// works); <c>review_required</c> when changes were requested on the post
    /// (<c>approval_status</c> <c>rejected</c>) and the update would schedule
    /// it (it needs a new review: create it again with
    /// <c>approval_workflow_id</c>; saving it as a draft works);
    /// <c>media_processing</c> as on <see cref="CreateAsync"/>.
    /// </summary>
    public Task<JsonElement?> UpdateAsync(string id, PostUpdateParams parameters, CancellationToken cancellationToken = default)
    {
        object body = parameters;
        if (parameters.RemoveCommentDmAutomation || parameters.RemoveDiscord)
        {
            // Built by hand: an explicit null comment_dm_automation
            // (RemoveCommentDmAutomation) or discord (RemoveDiscord) must
            // survive serialization, while unset fields are omitted.
            var node = JsonSerializer.SerializeToNode(parameters, OmniSocialsClient.SerializerOptions)!.AsObject();
            if (parameters.RemoveCommentDmAutomation)
            {
                node["comment_dm_automation"] = null;
            }
            if (parameters.RemoveDiscord)
            {
                node["discord"] = null;
            }
            body = node;
        }
        return _client.PatchAsync($"/posts/{Uri.EscapeDataString(id)}", body, cancellationToken);
    }

    /// <summary>
    /// <c>DELETE /posts/:id</c>: remove a post from OmniSocials (the live post
    /// stays on the platform). Resolves to null (204). While the post is
    /// publishing (status <c>posting</c>) or a retry publishes one of its
    /// channels, the answer is <c>409 post_publishing</c> and nothing is
    /// deleted: poll <see cref="GetAsync"/> until <c>published</c>,
    /// <c>warning</c> or <c>failed</c>, then delete it.
    /// </summary>
    public Task<JsonElement?> DeleteAsync(string id, CancellationToken cancellationToken = default)
        => _client.DeleteAsync($"/posts/{Uri.EscapeDataString(id)}", cancellationToken);

    /// <summary>
    /// <c>POST /posts/:id/publish</c>: publish a draft or scheduled post now. A
    /// post on which changes were requested in its approval workflow
    /// (<c>approval_status</c> <c>rejected</c>) answers
    /// <c>409 review_required</c>: it publishes only after a new review
    /// (create it again with <c>approval_workflow_id</c>).
    /// </summary>
    public Task<JsonElement?> PublishAsync(string id, CancellationToken cancellationToken = default)
        => _client.PostAsync($"/posts/{Uri.EscapeDataString(id)}/publish", null, cancellationToken);

    /// <summary>
    /// <c>POST /posts/:id/retry</c>: retry the failed platforms of a <c>failed</c>
    /// or <c>warning</c> (partially failed) post, on the same post. Only the
    /// platforms that failed are re-published; platforms that already succeeded
    /// are never posted again. Asynchronous: a 200 means the retry is queued -
    /// poll GetAsync for the outcome. Max 3 retries per platform. While another
    /// retry runs (a second request, the dashboard, the automatic retry) the
    /// answer is <c>409 retry_in_progress</c> and nothing is queued: poll
    /// GetAsync and retry once the post is <c>failed</c> or <c>warning</c> again.
    /// </summary>
    public Task<JsonElement?> RetryAsync(string id, CancellationToken cancellationToken = default)
        => _client.PostAsync($"/posts/{Uri.EscapeDataString(id)}/retry", null, cancellationToken);

    /// <summary>
    /// <c>POST /posts/:id/approve</c>: approve the current step of a post's
    /// approval workflow, on behalf of the user who owns this API key. That
    /// user must be a listed approver for the workflow's CURRENT step: steps
    /// approve in order, so an approver on a later step gets a 403
    /// <c>forbidden</c> error until earlier steps clear. Only works on a post
    /// with <c>approval_status: "pending"</c>. If this is the last step, the
    /// post finalizes immediately (<c>scheduled</c> or <c>posting</c>);
    /// otherwise it stays <c>in_approval</c> and the next step's approvers
    /// are notified. Without a step this approves whatever step is current;
    /// prefer <see cref="ApproveAsync(string, int?, CancellationToken)"/>.
    /// </summary>
    public Task<JsonElement?> ApproveAsync(string id, CancellationToken cancellationToken = default)
        => ApproveAsync(id, null, cancellationToken);

    /// <summary>
    /// <c>POST /posts/:id/approve</c> with <c>step_order</c>: the
    /// <c>current_step</c> of <see cref="GetApprovalAsync"/>, the step you
    /// reviewed (null leaves it out). When the review is on another step now
    /// (somebody decided since you read it, or a retried call already went
    /// through), nothing is approved and the call throws a 409
    /// <see cref="ApiException"/> with code <c>step_changed</c>; the step the
    /// review is on now is <c>error.current_step_order</c> in its body. Read
    /// the approval again before you decide. Without a step, a retry after a
    /// timeout can approve the next step too.
    /// </summary>
    public Task<JsonElement?> ApproveAsync(string id, int? stepOrder, CancellationToken cancellationToken = default)
        => _client.PostAsync(
            $"/posts/{Uri.EscapeDataString(id)}/approve",
            stepOrder is null ? null : new { step_order = stepOrder },
            cancellationToken);

    /// <summary>
    /// <c>POST /posts/:id/reject</c>: reject a post's approval workflow, on
    /// behalf of the user who owns this API key. Same approver requirement as
    /// <see cref="ApproveAsync(string, CancellationToken)"/>. Unlike approval,
    /// a rejection stops the WHOLE workflow immediately (not just the current
    /// step): the post's status becomes <c>rejected</c>.
    /// <paramref name="comment"/> is optional (max 2000 characters) and, when
    /// given, is shown to the requester and other approvers in the post's
    /// review thread.
    /// </summary>
    public Task<JsonElement?> RejectAsync(string id, string? comment = null, CancellationToken cancellationToken = default)
        => RejectAsync(id, comment, null, cancellationToken);

    /// <summary>
    /// <c>POST /posts/:id/reject</c> with a comment and <c>step_order</c>
    /// (both optional, pass null). <paramref name="stepOrder"/> works as on
    /// <see cref="ApproveAsync(string, int?, CancellationToken)"/>: when the
    /// review moved on, nothing is rejected and the call throws a 409
    /// <c>step_changed</c> <see cref="ApiException"/> with
    /// <c>current_step_order</c> in its body.
    /// </summary>
    public Task<JsonElement?> RejectAsync(string id, string? comment, int? stepOrder, CancellationToken cancellationToken = default)
        => _client.PostAsync(
            $"/posts/{Uri.EscapeDataString(id)}/reject",
            // Null fields are omitted, so a bare reject still sends {}.
            new { comment, step_order = stepOrder },
            cancellationToken);

    /// <summary>
    /// <c>GET /posts/:id/approval</c>: the approval review of a post - every
    /// step with its approvers and their decisions, the rejection with its
    /// reason, and the comment thread. Use it when <c>approval_status</c> is
    /// <c>rejected</c> to learn who rejected the post and why, or while it is
    /// <c>pending</c> to see who the post waits for. A post without an
    /// approval workflow returns <c>status: "none"</c> with empty
    /// <c>steps</c> and <c>comments</c>. Read-only; requires the
    /// <c>posts:read</c> scope. Deserialize into
    /// <see cref="PostApprovalResponse"/> for typed access.
    /// </summary>
    public Task<JsonElement?> GetApprovalAsync(string id, CancellationToken cancellationToken = default)
        => _client.GetAsync($"/posts/{Uri.EscapeDataString(id)}/approval", null, cancellationToken);
}
