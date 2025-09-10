let eventSource;
let reconnectAttempts = 0;
let reconnectTimer = null;
let dotNetRef = null;
let sseUrl = null;

const BASE_DELAY_MS = 1000;
const MAX_DELAY_MS = 10000;
const JITTER_MS = 500;

export function startSse(url, dotNetReference) {
    stopSse();

    sseUrl = url;
    dotNetRef = dotNetReference;
    reconnectAttempts = 0;

    connect();

    dotNetRef.invokeMethodAsync("OnSseStart");
}

function connect() {
    try {
        eventSource = new EventSource(sseUrl);

        eventSource.onopen = () => {
            if (reconnectAttempts === 0) {
                dotNetRef.invokeMethodAsync("OnSseConnect");
                console.info("SSE connected");
            } else {
                dotNetRef.invokeMethodAsync("OnSseReconnect", reconnectAttempts);
                console.info(`SSE connected after ${reconnectAttempts} attempts`);
            }

            reconnectAttempts = 0;
        };

        eventSource.onerror = () => {
            dotNetRef.invokeMethodAsync("OnSseError");
            console.warn("SSE error, scheduling reconnect...");
            scheduleReconnect();
        };

        eventSource.onmessage = e => {
            const type = e.type || "message"; // fallback if no event type
            const data = e.data;
            const id = e.lastEventId || null;

            dotNetRef.invokeMethodAsync("OnSseMessage", type, data, id);
        };
    } catch (err) {
        console.error("SSE connection failed:", err);
        scheduleReconnect();
    }
}

function scheduleReconnect() {
    if (reconnectTimer) return;

    const delay = Math.min(BASE_DELAY_MS * (reconnectAttempts + 1), MAX_DELAY_MS);
    const jitter = Math.floor(Math.random() * JITTER_MS);
    const totalDelay = delay + jitter;

    reconnectTimer = setTimeout(() => {
        reconnectTimer = null;
        reconnectAttempts++;

        dotNetRef.invokeMethodAsync("OnSseReconnect", reconnectAttempts);

        connect();

    }, totalDelay);

    console.info(`Reconnect attempt ${reconnectAttempts} in ${totalDelay}ms`);
}

export function stopSse() {
    if (eventSource) {
        eventSource.close();
        eventSource = null;
    }

    if (reconnectTimer) {
        clearTimeout(reconnectTimer);
        reconnectTimer = null;
    }

    dotNetRef.invokeMethodAsync("OnSseStop");

    reconnectAttempts = 0;
    dotNetRef = null;
    sseUrl = null;
}