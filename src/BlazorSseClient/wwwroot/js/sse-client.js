let eventSource;
let reconnectAttempts = 0;
let reconnectTimer = null;
let dotNetRef = null;
let sseUrl = null;
let isStopped = true;

const BASE_DELAY_MS = 1000;
const MAX_DELAY_MS = 10000;
const JITTER_MS = 500;

export function startSse(url, dotNetReference) {
    stopSse(); // ensures clean slate

    sseUrl = url;
    dotNetRef = dotNetReference;
    reconnectAttempts = 0;
    isStopped = false;

    connect();

    safeInvoke("OnSseStart");
}

function connect() {
    if (!dotNetRef || isStopped) return;

    try {
        eventSource = new EventSource(sseUrl);

        eventSource.onopen = () => {
            if (reconnectAttempts === 0) {
                safeInvoke("OnSseConnect");
                console.info("SSE connected");
            } else {
                // Successful reconnect after attempts
                safeInvoke("OnSseReconnect", reconnectAttempts);
                console.info(`SSE reconnected after ${reconnectAttempts} attempt(s)`);
            }

            reconnectAttempts = 0;
        };

        eventSource.onerror = () => {
            safeInvoke("OnSseError");
            console.warn("SSE error, scheduling reconnect...");
            scheduleReconnect();
        };

        eventSource.onmessage = e => {
            const type = e.type || "message";
            const data = e.data;
            const id = e.lastEventId || null;
            safeInvoke("OnSseMessage", type, data, id);
        };
    } catch (err) {
        console.error("SSE connection setup failed:", err);
        scheduleReconnect();
    }
}

function scheduleReconnect() {
    if (isStopped || reconnectTimer || !dotNetRef) return;

    const delay = Math.min(BASE_DELAY_MS * (reconnectAttempts + 1), MAX_DELAY_MS);
    const jitter = Math.floor(Math.random() * JITTER_MS);
    const totalDelay = delay + jitter;

    reconnectTimer = setTimeout(() => {
        reconnectTimer = null;
        if (isStopped || !dotNetRef) return;

        reconnectAttempts++;
        // Notify attempt phase (pre-open)
        safeInvoke("OnSseReconnectAttempt", reconnectAttempts);

        connect();
    }, totalDelay);

    console.info(`Reconnect attempt #${reconnectAttempts + 1} scheduled in ${totalDelay}ms`);
}

export function stopSse() {
    isStopped = true;

    if (eventSource) {
        try {
            eventSource.close();
        } catch {
            /* ignore - already in the desired state */
        }

        eventSource = null;
    }

    if (reconnectTimer) {
        clearTimeout(reconnectTimer);
        reconnectTimer = null;
    }

    if (dotNetRef) {
        safeInvoke("OnSseStop");
    }

    reconnectAttempts = 0;
    dotNetRef = null;
    sseUrl = null;
}

function safeInvoke(method, ...args) {
    try {
        if (dotNetRef) {
            dotNetRef.invokeMethodAsync(method, ...args);
        }
    } catch (e) {
        // Swallow; invoking after dispose can throw.
        console.debug(`Invoke ${method} suppressed:`, e);
    }
}