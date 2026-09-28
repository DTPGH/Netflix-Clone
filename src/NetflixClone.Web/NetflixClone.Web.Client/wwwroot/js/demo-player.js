// Keep a reference until explicit cleanup, even if Blazor has detached the element.
const states = new Map();
export function stop(video, id) {
    const state = states.get(id);
    video = state?.video ?? video;
    if (!video) return;
    if (state) {
        clearTimeout(state.timer);
        video.removeEventListener("error", state.error);
        video.removeEventListener("loadedmetadata", state.metadata);
        document.removeEventListener("visibilitychange", state.visible);
        states.delete(id);
    }
    video.pause();
    video.removeAttribute("src");
    video.load();
}
export function setSource(video, url, expiresAt, owner, version, id) {
    const old = states.get(id);
    const position = old && Number.isFinite(video.currentTime) ? video.currentTime : 0;
    const resume = old && !video.paused && !video.ended;
    stop(video, id);
    const expires = Date.parse(expiresAt);
    const state = { video, renewing: false, timer: null, metadata: null, error: null, visible: null };
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
    };
    state.metadata = () => {
        if (position > 0 && Number.isFinite(video.duration)) video.currentTime = Math.min(position, video.duration);
        if (resume) video.play().catch(() => {});
    };
    states.set(id, state);
    video.addEventListener("error", state.error);
    video.addEventListener("loadedmetadata", state.metadata, { once: true });
    document.addEventListener("visibilitychange", state.visible);
    state.timer = setTimeout(renew, Math.max(1000, expires - Date.now() - 20000));
    video.src = url;
    video.load();
}
