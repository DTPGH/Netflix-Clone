export function consumeLink() {
    const values = new URLSearchParams(window.location.hash.slice(1));
    const result = { accountId: Number(values.get("accountId")), token: values.get("token") || "" };
    // Fragment is not sent to the server; remove it from the current history entry.
    history.replaceState(null, "", window.location.pathname + window.location.search);
    return result;
}
