// Keep a reference until explicit cleanup, even if Blazor has detached the element.
const states = new Map();
export function stop(video, id, preserveProgress = false) {
    const state = states.get(id);
    video = state?.video ?? video;
    if (!video) return;
    if (state) {
        clearTimeout(state.timer);
        video.removeEventListener("error", state.error);
        video.removeEventListener("loadedmetadata", state.metadata);
        video.removeEventListener("playing", state.playing);
        video.removeEventListener("pause", state.pause);
        video.removeEventListener("ended", state.ended);
        video.removeEventListener("timeupdate", state.timeupdate);
        if (!preserveProgress) state.progress.cancelled = true;
        document.removeEventListener("visibilitychange", state.visible);
        states.delete(id);
    }
    video.pause();
    video.removeAttribute("src");
    video.load();
}
export function setSource(video, url, expiresAt, owner, version, id, resumeSeconds = 0) {
    const old = states.get(id);
    const wasEnded = old && video.ended;
    const position = wasEnded ? 0 : old && Number.isFinite(video.currentTime) ? video.currentTime : resumeSeconds;
    const resume = old && !video.paused && !video.ended;
    stop(video, id, true);
    const expires = Date.parse(expiresAt);
    const progress = old?.progress ?? { played: false, cancelled: false, pending: null, flight: null, last: Date.now() };
    // Renewing an ended source must not turn a passive pause/flush into a replay checkpoint.
    if (wasEnded) progress.played = false;
    const state = { video, progress, ready: false, renewing: false, timer: null, metadata: null, error: null, visible: null };
    state.report = () => {
        if (!state.ready || !progress.played || progress.cancelled || !Number.isFinite(video.duration) ||
            video.duration <= 0 || video.duration > 86400 || !Number.isFinite(video.currentTime))
            return progress.flight ?? Promise.resolve();
        progress.pending = [Math.max(0, Math.floor(Math.min(video.currentTime, video.duration))),
            Math.ceil(video.duration), video.ended];
        progress.last = Date.now();
        if (!progress.flight) {
            progress.flight = (async () => {
                while (progress.pending && !progress.cancelled) {
                    const sample = progress.pending;
                    progress.pending = null;
                    await owner.invokeMethodAsync("ReportProgress", version, ...sample);
                }
            })().catch(() => {}).finally(() => { progress.flight = null; });
        }
        return progress.flight;
    };
    state.playing = () => { progress.played = true; };
    state.pause = () => { void state.report(); };
    state.ended = () => { void state.report(); };
    state.timeupdate = () => {
        if (!video.paused && Date.now() - progress.last >= 15000) void state.report();
    };
    const renew = () => {
        if (state.renewing || states.get(id) !== state) return;
        state.renewing = true;
        owner.invokeMethodAsync("RenewPlayback", version).catch(() => {});
    };
    state.error = () => {
        if (Date.now() >= expires - 20000) renew();
        else owner.invokeMethodAsync("PlaybackFailed", version).catch(() => {});
    };
    state.visible = () => {
        if (document.visibilityState === "visible" && Date.now() >= expires - 20000) renew();
        if (document.visibilityState === "hidden") void state.report();
    };
    state.metadata = () => {
        if (position > 0 && Number.isFinite(video.duration))
            video.currentTime = Math.min(position, Math.max(0, video.duration - 1));
        state.ready = true;
        if (resume) video.play().catch(() => {});
    };
    states.set(id, state);
    video.addEventListener("error", state.error);
    video.addEventListener("loadedmetadata", state.metadata, { once: true });
    video.addEventListener("playing", state.playing);
    video.addEventListener("pause", state.pause);
    video.addEventListener("ended", state.ended);
    video.addEventListener("timeupdate", state.timeupdate);
    document.addEventListener("visibilitychange", state.visible);
    state.timer = setTimeout(renew, Math.max(1000, expires - Date.now() - 20000));
    video.src = url;
    video.load();
}
export async function flush(id) {
    const state = states.get(id);
    if (state) await state.report();
}
