// A stream slot is acquired by /playback before an MP4 source is assigned.
// Watched time is separate from lease heartbeats and from resume position.
export function createViewingTracker(video, owner, version, initial) {
    if (!initial || !Number.isInteger(initial.sessionId) || initial.sessionId < 1)
        throw new Error("A playback reservation is required.");
    const sessionId = initial.sessionId;
    let total = initial.watchedMilliseconds, sequence = initial.sequence;
    let expires = Date.parse(initial.leaseExpiresAtUtc);
    let pending = null, flight = null, stopped = false, detached = true, active = false;
    let closed = initial.isEnded, expired = false, restarting = false, terminalReason = null;
    let last = performance.now(), position = video.currentTime || 0, ticks = 0;
    const sample = () => {
        const now = performance.now(), current = video.currentTime || 0;
        if (active && !detached && !video.seeking && video.readyState >= 3) {
            const advanced = Math.max(0, (current - position) * 1000 / Math.max(0.1, video.playbackRate));
            total += Math.min(Math.max(0, now - last), advanced, 2000);
        }
        last = now; position = current;
    };
    const expire = () => {
        if (expired || stopped || terminalReason) return;
        expired = true; active = false; video.pause();
        void owner.invokeMethodAsync("PlaybackLeaseExpired", version).catch(() => {});
    };
    const hasLease = () => !closed && !expired && Number.isFinite(expires) && Date.now() < expires;
    const send = () => {
        if (flight) return flight;
        flight = (async () => {
            while (pending && !closed) {
                const checkpoint = pending;
                const reply = await owner.invokeMethodAsync("SaveViewingCheckpoint", version, sessionId,
                    checkpoint.sequence, checkpoint.total, checkpoint.reason);
                if (!reply) break; // Playing retries; a pause must never keep renewing a slot.
                if (reply.sessionId !== sessionId) { expire(); break; }
                expires = Math.max(expires, Date.parse(reply.leaseExpiresAtUtc));
                if (pending === checkpoint) pending = null;
                if (reply.isEnded) {
                    closed = true;
                    if (!terminalReason) expire();
                    break;
                }
            }
        })().catch(() => {}).finally(() => { flight = null; });
        return flight;
    };
    const report = (reason = null) => {
        sample();
        if (reason && !terminalReason) terminalReason = reason;
        if (!closed) pending = { sequence: ++sequence, total: Math.floor(total), reason: terminalReason };
        return send();
    };
    const play = () => {
        if (stopped || detached) return;
        if ((closed || terminalReason === "Finished") && !expired) {
            video.pause();
            if (!restarting) {
                restarting = true;
                // An immediate replay may arrive before the final checkpoint response.
                // Wait for that close before requesting another slot, especially on a one-stream plan.
                void report("Finished").then(() => owner.invokeMethodAsync("RestartPlayback", version))
                    .catch(() => { restarting = false; expire(); });
            }
        } else if (!hasLease()) { video.pause(); expire(); }
    };
    const playing = () => {
        if (terminalReason === "Finished" || !hasLease()) { play(); return; }
        active = true; last = performance.now(); position = video.currentTime || 0;
        // Confirm liveness on resume; zero paused time is added to the counter.
        void report();
    };
    const pause = () => { sample(); active = false; if (!stopped && !closed && !expired) void report(); };
    const waiting = () => { sample(); active = false; };
    const seeking = () => { sample(); active = false; };
    const seeked = () => { last = performance.now(); position = video.currentTime || 0; active = hasLease() && !video.paused && video.readyState >= 3; };
    const ended = () => { sample(); active = false; void report("Finished"); };
    const pagehide = () => { void report("Closed"); };
    const visible = () => { if (document.visibilityState === "visible" && !closed && !hasLease()) expire(); };
    const handlers = { play, playing, pause, waiting, seeking, seeked, ended };
    const attach = () => {
        if (stopped) return;
        detached = false; last = performance.now(); position = video.currentTime || 0;
        for (const [name, handler] of Object.entries(handlers)) video.addEventListener(name, handler);
        window.addEventListener("pagehide", pagehide);
        document.addEventListener("visibilitychange", visible);
    };
    const detach = () => {
        sample(); active = false; detached = true;
        for (const [name, handler] of Object.entries(handlers)) video.removeEventListener(name, handler);
        window.removeEventListener("pagehide", pagehide);
        document.removeEventListener("visibilitychange", visible);
    };
    const timer = setInterval(() => {
        if (stopped || detached) return;
        sample();
        if (!closed && !hasLease()) { expire(); return; }
        if (++ticks % 15 === 0 && active && !video.paused && !video.ended) void report();
    }, 1000);
    attach();
    return {
        sessionId, attach, detach,
        updateReservation(reply) { if (reply.sessionId === sessionId) expires = Math.max(expires, Date.parse(reply.leaseExpiresAtUtc)); },
        async close() {
            stopped = true; detach(); clearInterval(timer);
            await report("Closed");
        }
    };
}
