using System.Text.Json.Serialization;

namespace OmniSocials;

/// <summary>Body for <c>PATCH /accounts/:id</c>.</summary>
public sealed class AccountUpdateParams
{
    /// <summary>
    /// The new X statistics setting. Always sent: null goes out as
    /// <c>"x_statistics": null</c> (OmniSocials Connect keys only: the account
    /// follows its customer's setting again).
    /// </summary>
    [JsonPropertyName("x_statistics")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public XStatisticsInput? XStatistics { get; set; }
}

/// <summary>
/// The X statistics change of <see cref="AccountsResource.UpdateAsync"/>. Set one
/// property or more; a null property is left out and keeps its value.
/// </summary>
public sealed class XStatisticsInput
{
    /// <summary>
    /// How often each post is checked: <c>full</c> (5 checks: about 5 hours, day
    /// 1, 3 and 7, and day 30 for posts with 100+ views), <c>light</c> (2 checks:
    /// about 5 hours and day 7), <c>off</c>, or <c>custom</c> (OmniSocials Connect
    /// keys only, with <see cref="PostSchedule"/>).
    /// </summary>
    [JsonPropertyName("posts")]
    public string? Posts { get; set; }

    /// <summary>
    /// How often the follower count is read: <c>daily</c>, <c>weekly</c> or
    /// <c>off</c>; OmniSocials Connect keys also <c>hourly</c> and <c>6h</c>.
    /// </summary>
    [JsonPropertyName("followers")]
    public string? Followers { get; set; }

    /// <summary>OmniSocials Connect keys only, with <c>Posts = "custom"</c>.</summary>
    [JsonPropertyName("post_schedule")]
    public XStatisticsPostSchedule? PostSchedule { get; set; }
}

/// <summary>
/// A custom check schedule (OmniSocials Connect keys). <see cref="At"/>: times
/// after publishing such as <c>1h</c>, <c>1d</c>, <c>7d</c> (at most 24, 1h to
/// 365d, at least 1 hour apart); <see cref="ThenEvery"/>: repeat after the last
/// one (1d to 30d); <see cref="Until"/>: stop repeating after this age.
/// </summary>
public sealed class XStatisticsPostSchedule
{
    [JsonPropertyName("at")]
    public IList<string> At { get; set; } = new List<string>();

    [JsonPropertyName("then_every")]
    public string? ThenEvery { get; set; }

    [JsonPropertyName("until")]
    public string? Until { get; set; }
}
