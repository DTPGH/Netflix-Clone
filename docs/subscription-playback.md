# Subscription playback eligibility

Playback requires an account-owned subscription with Status = Active, StartDate <= IClock.UtcNow and EndDate > IClock.UtcNow. A null EndDate does not grant access. Plan.IsActive is deliberately excluded: stopping sales does not cancel purchased access.

GetMoviePlaybackUseCase keeps account eligibility, profile ownership/deletion and maturity checks, then checks subscription eligibility through ISubscriptionRepository.HasEffectiveAsync. Infrastructure uses an AsNoTracking AnyAsync query. This verification performs no database writes and does not change subscription status at expiry.

Both playback-ticket issuance and every new MP4 GET/HEAD/Range request call this use case. Missing entitlement returns Movies.SubscriptionRequired / HTTP 403. Watch progress and viewing-session operations that reuse this eligibility check also map Forbidden to 403. Browse, detail, history listing and trailers remain unchanged. Admin has no playback exemption.

Watch displays a subscription link when ticket issuance is denied. Already downloaded/buffered bytes or an ongoing response cannot be withdrawn at the expiry instant; new media requests are denied. Stream limits and quality restrictions remain outside this slice.

Verification: solution build and Playback.Specs, including invalid status, other account, null end, future start, inclusive start/exclusive end, valid ticket after expiry, GET/HEAD/Range denial, discontinued plan and no writes. HTTP tests use fake persistence; SQL Server integration and browser playback require manual verification.
