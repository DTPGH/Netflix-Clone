// Separate from resume position: seeking never grants watched time.
export function createViewingTracker(video, owner, version) {
    let session = null, clientId = crypto.randomUUID(), total = 0, sequence = 0;
    let pending = null, flight = null, closed = false, stopped = false, active = false, terminalReason = null;
    let last = performance.now(), position = video.currentTime || 0;
    const sample = () => {
        const now = performance.now(), current = video.currentTime || 0;
        const elapsed = Math.max(0, now - last);
        if (session && active && !video.seeking && video.readyState >= 3) {
            // Cap gaps: background throttling / sleep is not automatically credited.
            const advanced = Math.max(0, (current - position) * 1000 / Math.max(0.1, video.playbackRate));
            total += Math.min(elapsed, advanced, 2000);
        }
        last = now; position = current;
    };
    const send = async () => {
        if (flight) return flight;
        flight = (async () => {
            if (!session && !closed && !stopped && active) {
                const reply = await owner.invokeMethodAsync("StartViewingSession", version, clientId);
                if (reply && !stopped) {
                    if (reply.isEnded) { clientId = crypto.randomUUID(); return; }
                    session = reply.sessionId; sequence = reply.sequence;
                    total = reply.watchedSeconds * 1000; last = performance.now(); position = video.currentTime || 0;
                    if (terminalReason) pending = { sequence: ++sequence, total: Math.floor(total), reason: terminalReason };
                }
            }
            if (!session) return;
            while (pending) {
                const checkpoint = pending;
                const reply = await owner.invokeMethodAsync("SaveViewingCheckpoint", version, session,
                    checkpoint.sequence, checkpoint.total, checkpoint.reason);
                if (!reply) break; // Retain the checkpoint for a later retry, including a terminal reason.
                if (pending === checkpoint) pending = null;
                if (reply.isEnded) { closed = true; break; }
            }
        })().catch(() => {}).finally(() => { flight = null; });
        return flight;
    };
    const report = (reason = null) => {
        sample();
        if (reason && !terminalReason) terminalReason = reason;
        if (session && !closed) pending = { sequence: ++sequence, total: Math.floor(total), reason: terminalReason };
        return send();
    };
    const playing = async () => {
        if (closed) { session = null; clientId = crypto.randomUUID(); total = sequence = 0; pending = null; terminalReason = null; closed = false; }
        active = true; last = performance.now(); position = video.currentTime || 0;
        await send();
    };
    const pause = () => { sample(); active = false; void report(); };
    const waiting = () => { sample(); active = false; };
    const seeking = () => { sample(); active = false; };
    const seeked = () => { last = performance.now(); position = video.currentTime || 0; active = !video.paused && video.readyState >= 3; };
    const ended = () => { sample(); active = false; void report("Finished"); };
    const pagehide = () => { void report("Closed"); };
    const handlers = { playing, pause, waiting, seeking, seeked, ended };
    const attach = () => {
        stopped = false; last = performance.now(); position = video.currentTime || 0;
        for (const [name, handler] of Object.entries(handlers)) video.addEventListener(name, handler);
        window.addEventListener("pagehide", pagehide);
    };
    const detach = () => {
        sample(); active = false;
        for (const [name, handler] of Object.entries(handlers)) video.removeEventListener(name, handler);
        window.removeEventListener("pagehide", pagehide);
    };
    let ticks = 0;
    const timer = setInterval(() => {
        sample();
        if (closed && active && !video.paused && !video.ended && !stopped) void playing();
        if (++ticks % 15 === 0) void report(); // Includes paused heartbeats; no paused time is credited.
    }, 1000);
    attach();
    return {
        attach, detach,
        async close() {
            // Stop activity before awaiting the final response: it must not create a new session during teardown.
            stopped = true; detach(); clearInterval(timer);
            await report("Closed");
        }
    };
}
