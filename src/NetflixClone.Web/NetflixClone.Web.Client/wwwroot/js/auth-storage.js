const credentialKey = "netflixclone.auth.refresh.v1";
const deviceKey = "netflixclone.auth.device.v1";
const epochKey = "netflixclone.auth.epoch.v1";
const leases = new Map();
let nextId = 0;
let listener;
let resumeListener;

export function listen(receiver) {
    // Fail safely instead of silently allowing concurrent rotation in unsupported browsers.
    if (!navigator.locks) throw new Error("Browser session coordination is unavailable.");
    const probe = "netflixclone.storage.probe";
    localStorage.setItem(probe, "1");
    localStorage.removeItem(probe);
    listener = event => {
        if (event.key === epochKey || event.key === null)
            receiver.invokeMethodAsync("OnSessionChanged").catch(() => {});
    };
    window.addEventListener("storage", listener);
    resumeListener = () => {
        if (document.visibilityState === "visible")
            receiver.invokeMethodAsync("OnResumed").catch(() => {});
    };
    document.addEventListener("visibilitychange", resumeListener);
    window.addEventListener("focus", resumeListener);
}
export function unlisten() {
    window.removeEventListener("storage", listener);
    document.removeEventListener("visibilitychange", resumeListener);
    window.removeEventListener("focus", resumeListener);
}
export function acquire() {
    return new Promise((resolve, reject) => {
        navigator.locks.request("netflixclone.auth.session.v1", async () => {
            const id = ++nextId;
            await new Promise(release => { leases.set(id, release); resolve(id); });
        }).catch(reject);
    });
}
export function release(id) { const release = leases.get(id); leases.delete(id); release?.(); }
export function read() {
    const value = localStorage.getItem(credentialKey);
    if (!value) return null;
    try {
        const data = JSON.parse(value);
        if (typeof data.refreshToken !== "string" || !/^[0-9A-F]{64}$/.test(data.refreshToken) ||
            typeof data.refreshTokenExpiresAtUtc !== "string" ||
            !Number.isFinite(Date.parse(data.refreshTokenExpiresAtUtc))) throw new Error();
        return data;
    } catch { localStorage.removeItem(credentialKey); return null; }
}
export function write(value) { localStorage.setItem(credentialKey, JSON.stringify(value)); }
export function clear() { localStorage.removeItem(credentialKey); }
export function getDevice() { return localStorage.getItem(deviceKey); }
export function setDevice(value) { localStorage.setItem(deviceKey, value); }
export function getEpoch() { return localStorage.getItem(epochKey) ?? ""; }
export function changeEpoch() { localStorage.setItem(epochKey, crypto.randomUUID()); }
