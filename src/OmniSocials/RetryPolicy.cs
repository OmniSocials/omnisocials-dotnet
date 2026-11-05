namespace OmniSocials;

/// <summary>Which failures a request is sent again on.</summary>
internal enum RetryPolicy
{
    /// <summary>Retry a 429, a 5xx and a connection error or timeout (the default).</summary>
    Default,

    /// <summary>
    /// Retry only a 429. A call that can start or raise ad spend uses it: after a
    /// 5xx, a connection error or a timeout the first attempt may have gone
    /// through, so it is never sent twice.
    /// </summary>
    RateLimitOnly,
}
