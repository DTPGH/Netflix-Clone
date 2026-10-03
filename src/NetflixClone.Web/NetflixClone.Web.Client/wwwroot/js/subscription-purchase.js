const key = account => "netflix.subscription-purchase." + account;
export function read(account) {
    const text = sessionStorage.getItem(key(account));
    if (!text) return null;
    let value;
    try { value = JSON.parse(text); } catch { sessionStorage.removeItem(key(account)); return null; }
    if (!Number.isInteger(value.planId) || value.planId <= 0 || typeof value.planName !== "string" ||
        value.planName.length > 100 || typeof value.displayPrice !== "number" || value.displayPrice < 0 ||
        typeof value.idempotencyKey !== "string" || !/^[0-9a-f]{8}(-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i.test(value.idempotencyKey) ||
        typeof value.expectedPlanUpdatedAtUtc !== "string" || !Number.isFinite(Date.parse(value.expectedPlanUpdatedAtUtc)) ||
        typeof value.submitted !== "boolean") {
        sessionStorage.removeItem(key(account)); return null;
    }
    return value;
}
export function write(account, draft) { sessionStorage.setItem(key(account), JSON.stringify(draft)); }
export function clear(account) { sessionStorage.removeItem(key(account)); }
