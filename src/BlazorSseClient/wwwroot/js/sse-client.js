let eventSource;
let reconnectAttempts = 0;
let reconnectTimer = null;
let dotNetRef = null;
let currentUrl = null;

let cfg = {
    reconnectBaseDelayMs: 1000,
    reconnectMaxDelayMs: 10000,
    reconnectJitterMs: 500,
    useCredentials: false
};

const STATE_RUN_CHANGE_METHOD = 'OnSseRunStateChange';
const STATE_RUN_NAMES = ['Unknown', 'Starting', 'Started', 'Stopping', 'Stopped'];
const STATE_RUN_UNKNOWN = 0;
const STATE_RUN_STARTING = 1;
const STATE_RUN_STARTED = 2;
const STATE_RUN_STOPPING = 3;
const STATE_RUN_STOPPED = 4;

const STATE_CONNECTION_CHANGE_METHOD = 'OnSseConnectionStateChange';
const STATE_CONNECTION_NAMES = ['Unknown', 'Opening', 'Open', 'Reopening', 'Reopened', 'Closed'];
const STATE_CONNECTION_UNKNOWN = 0;
const STATE_CONNECTION_OPENING = 1;
const STATE_CONNECTION_OPEN = 2;
const STATE_CONNECTION_REOPENING = 3;
const STATE_CONNECTION_REOPENED = 4;    //  Not actually a state, just a notification. Reopened goes to Opened.
const STATE_CONNECTION_CLOSED = 5;

let runState = STATE_RUN_STOPPED;
let connectionState = STATE_CONNECTION_CLOSED;

function log(...args) {
    console.debug("[SSE]", ...args);
}

function warn(...args) {
    if (cfg.debug) {
        console.warn("[SSE]", ...args);
    }
}

export function startSse(url, dotNetReference, options) {
    stopSse(); // ensures clean slate

    runStateChanged(STATE_RUN_STARTING, true);
    currentUrl = url;
    dotNetRef = dotNetReference;

    if (options) {
        cfg.reconnectBaseDelayMs = options.reconnectBaseDelayMs ?? cfg.reconnectBaseDelayMs;
        cfg.reconnectMaxDelayMs = options.reconnectMaxDelayMs ?? cfg.reconnectMaxDelayMs;
        cfg.reconnectJitterMs = options.reconnectJitterMs ?? cfg.reconnectJitterMs;
        cfg.useCredentials = !!options.useCredentials;
    }

    connect();

    runStateChanged(STATE_RUN_STARTED, true);
}

export function getRunState() {
    return runState;
}

export function getConnectionState() {
    return connectionState;
}

function runStateChanged(newState, broadcast) {
    runState = newState;

    if (broadcast && dotNetRef) {
        dotNetRef.invokeMethodAsync(STATE_RUN_CHANGE_METHOD, runState);
    }
}

function connectionStateChanged(newState, broadcast) {
    const priorState = connectionState;
    connectionState = newState;

    if (broadcast && dotNetRef) {
        switch (connectionState) {
            case STATE_CONNECTION_OPEN:
                if (priorState !== STATE_CONNECTION_OPEN && priorState !== STATE_CONNECTION_REOPENING) {
                    safeInvoke(STATE_CONNECTION_CHANGE_METHOD, STATE_CONNECTION_OPEN);
                } else if (priorState === STATE_CONNECTION_REOPENING) {
                    safeInvoke(STATE_CONNECTION_CHANGE_METHOD, STATE_CONNECTION_REOPENED);
                }

                break;

            case STATE_CONNECTION_REOPENING:
                if (priorState !== STATE_CONNECTION_REOPENING) {
                    safeInvoke(STATE_CONNECTION_CHANGE_METHOD, STATE_CONNECTION_REOPENING);
                }

                break;

            default:
                safeInvoke(STATE_CONNECTION_CHANGE_METHOD, connectionState);

                break;
        }
    }
}

function connect() {
    if (!dotNetRef) return;
    if (!currentUrl) return;
    if (runState !== STATE_RUN_STARTING && runState !== STATE_RUN_STARTED) return;
    if (connectionState !== STATE_CONNECTION_CLOSED && connectionState !== STATE_CONNECTION_REOPENING) return;

    if (reconnectAttempts === 0) {
        connectionStateChanged(STATE_CONNECTION_OPENING, true);
    }

    // This is set to 1 because we only want to a single "reopening" event,
    // even if we have multiple reconnect attempts. The purpose of the state
    // change is to notify the app that the connection was lost, and is now
    // being re-established. We don't need to notifiy it again on subsequent
    // attempts.
    if (reconnectAttempts === 1) {
        connectionStateChanged(STATE_CONNECTION_REOPENING, true);
    }

    try {
        eventSource = new EventSource(currentUrl, { withCredentials: cfg.useCredentials });

        eventSource.onopen = () => {
            log('connection opened');
            connectionStateChanged(STATE_CONNECTION_OPEN, true);
            reconnectAttempts = 0;
        };

        eventSource.onerror = () => {
            log('connection error');
            connectionStateChanged(STATE_CONNECTION_CLOSED, true);

            scheduleReconnect();
        };

        eventSource.onmessage = e => {
            const type = e.type || 'message';
            const data = e.data;
            const id = e.lastEventId || null;

            safeInvoke('OnSseMessage', type, data, id);
        };
    } catch (err) {
        console.error('SSE connection setup failed:', err);
        scheduleReconnect();
    }
}

function scheduleReconnect() {
    if (runState === STATE_RUN_STOPPED || reconnectTimer || !dotNetRef) return;

    warn('scheduling reconnect...');
    connectionStateChanged(STATE_CONNECTION_REOPENING, true);

    const delay = Math.min(cfg.reconnectBaseDelayMs * (reconnectAttempts + 1), cfg.reconnectMaxDelayMs);
    const jitter = Math.floor(Math.random() * cfg.reconnectJitterMs);
    const totalDelay = delay + jitter;

    reconnectTimer = setTimeout(() => {
        reconnectTimer = null;

        if (runState === STATE_RUN_STOPPED || !dotNetRef) return;

        reconnectAttempts++;

        connect();
    }, totalDelay);

    console.info(`Reconnect attempt #${reconnectAttempts + 1} scheduled in ${totalDelay}ms`);
}

export function stopSse() {
    if (runState === STATE_RUN_STOPPED) return;

    runStateChanged(STATE_RUN_STOPPING, true);

    if (eventSource) {
        try {
            eventSource.close();
            connectionStateChanged(STATE_CONNECTION_CLOSED, true);
        } catch {
            /* ignore - already in the desired state */
            connectionStateChanged(STATE_CONNECTION_CLOSED, false);
        }

        eventSource = null;
    }

    if (reconnectTimer) {
        clearTimeout(reconnectTimer);
        reconnectTimer = null;
    }

    if (dotNetRef) {
        runStateChanged(STATE_RUN_STOPPED, true);
    }

    reconnectAttempts = 0;
    dotNetRef = null;
    currentUrl = null;
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