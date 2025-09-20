let eventSource;
let reconnectAttempts = 0;
let reconnectTimer = null;
let dotNetRef = null;
let currentUrl = null;

let cfg = {
    reconnectBaseDelayMs: 1000,
    reconnectMaxDelayMs: 10000,
    reconnectJitterMs: 500,
    useCredentials: false,
    debug: false
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
const STATE_CONNECTION_REOPENED = 4;    // Not a state; notification only.
const STATE_CONNECTION_CLOSED = 5;

let runState = STATE_RUN_STOPPED;
let connectionState = STATE_CONNECTION_CLOSED;

function log(...args) {
    if (cfg.debug) {
        console.debug("[SSE]", ...args);
    }
}

function warn(...args) {
    if (cfg.debug) {
        console.warn("[SSE]", ...args);
    }
}

// Subscriptions map: messageType => listener function
const subscriptions = new Map();

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
    if (!eventSource)
        return STATE_CONNECTION_CLOSED;

    if (eventSource.readyState === 0)
        return STATE_CONNECTION_OPENING;

    if (eventSource.readyState === 1)
        return STATE_CONNECTION_OPEN;

    if (eventSource.readyState === 2)
        return STATE_CONNECTION_CLOSED;

    return STATE_CONNECTION_UNKNOWN;
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

function attachAllSubscriptions() {
    if (!eventSource) return;

    for (const [messageType, listener] of subscriptions.entries()) {
        try {
            eventSource.addEventListener(messageType, listener);
        } catch (e) {
            if (cfg.debug) console.debug(`Failed to attach listener for "${messageType}":`, e);
        }
    }
}

function detachAllSubscriptions() {
    if (!eventSource) return;

    for (const [messageType, listener] of subscriptions.entries()) {
        try {
            eventSource.removeEventListener(messageType, listener);
        } catch (e) {
            // ignore
        }
    }
}

/*
 Exports for dynamic subscription management:
 - subscribe(messageType): begin listening for server-sent events with this event name.
 - unsubscribe(messageType): stop listening for that event.
 - clearSubscriptions(): remove all subscriptions (detaches listeners and clears internal map).
 Note: Subscriptions persist across focus/visibility changes (no automatic cleanup).
*/
export function subscribe(messageType) {
    if (!messageType) return;

    // Avoid duplicates
    if (subscriptions.has(messageType)) return;

    const listener = function(e) {
        // Use the subscription's messageType rather than relying on e.type
        const data = e.data;
        const id = e.lastEventId || null;

        safeInvoke('OnSseMessage', messageType, data, id);
    };

    subscriptions.set(messageType, listener);

    if (eventSource) {
        try {
            eventSource.addEventListener(messageType, listener);
        } catch (e) {
            if (cfg.debug) console.debug(`subscribe: failed to addEventListener "${messageType}":`, e);
        }
    }
}

export function unsubscribe(messageType) {
    if (!messageType) return;

    const listener = subscriptions.get(messageType);

    if (!listener) return;

    if (eventSource) {
        try {
            eventSource.removeEventListener(messageType, listener);
        } catch (e) {
            // ignore
        }
    }

    subscriptions.delete(messageType);
}

export function clearSubscriptions() {
    // Detach listeners from the active EventSource if present, then clear the map.
    try {
        detachAllSubscriptions();
    } catch {
        // ignore
    } finally {
        subscriptions.clear();
    }
}

function cleanupEventSource() {
    if (!eventSource) return;

    try {
        // Detach named event listeners added via addEventListener
        detachAllSubscriptions();

        // Detach handlers to help GC and avoid stray callbacks
        eventSource.onopen = null;
        eventSource.onerror = null;
        eventSource.onmessage = null;
        eventSource.close();
    } catch {
        // ignore
    } finally {
        eventSource = null;
    }
}

function connect() {
    if (!dotNetRef) return;
    if (!currentUrl) return;
    if (runState !== STATE_RUN_STARTING && runState !== STATE_RUN_STARTED) return;
    if (connectionState !== STATE_CONNECTION_CLOSED && connectionState !== STATE_CONNECTION_REOPENING) return;

    // Ensure we never have two concurrent EventSource instances
    cleanupEventSource();

    if (reconnectAttempts === 0) {
        connectionStateChanged(STATE_CONNECTION_OPENING, true);
    }

    // Announce a single "reopening" per outage
    if (reconnectAttempts === 1) {
        connectionStateChanged(STATE_CONNECTION_REOPENING, true);
    }

    try {
        eventSource = new EventSource(currentUrl, { withCredentials: cfg.useCredentials });

        // Attach any previously registered subscriptions immediately so dynamic subscriptions work post-connect.
        attachAllSubscriptions();

        eventSource.onopen = () => {
            log('connection opened');

            connectionStateChanged(STATE_CONNECTION_OPEN, true);

            reconnectAttempts = 0;
        };

        eventSource.onerror = () => {
            log('connection error');

            // Move to closed and take over reconnection logic (disable native auto-retry)
            connectionStateChanged(STATE_CONNECTION_CLOSED, true);

            // Explicitly stop the browser's auto-reconnect to avoid overlapping connections
            cleanupEventSource();

            scheduleReconnect();
        };

        //// Default SSE messages
        //eventSource.onmessage = e => {
        //    const type = e.type || 'message';
        //    const data = e.data;
        //    const id = e.lastEventId || null;

        //    safeInvoke('OnSseMessage', type, data, id);
        //};
    } catch (err) {
        console.error('SSE connection setup failed:', err);
        cleanupEventSource();
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
        log(`Reconnect attempt #${reconnectAttempts} starting`);
        connect();
    }, totalDelay);

    if (cfg.debug) {
        console.info(`Reconnect attempt #${reconnectAttempts + 1} scheduled in ${totalDelay}ms`);
    }
}

export function stopSse() {
    if (runState === STATE_RUN_STOPPED) return;

    runStateChanged(STATE_RUN_STOPPING, true);

    // Stop reconnect attempts
    if (reconnectTimer) {
        clearTimeout(reconnectTimer);
        reconnectTimer = null;
    }

    // Close the current EventSource (and detach handlers)
    cleanupEventSource();

    // Update connection state
    connectionStateChanged(STATE_CONNECTION_CLOSED, true);

    // Always move to STOPPED internally; only broadcast if there's a .NET listener
    const canBroadcast = !!dotNetRef;
    runStateChanged(STATE_RUN_STOPPED, canBroadcast);

    reconnectAttempts = 0;

    // Release the .NET ref last
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
        if (cfg.debug) {
            console.debug(`Invoke ${method} suppressed:`, e);
        }
    }
}