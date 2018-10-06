class Bridge {
    constructor() {
        this.callbacks = {};
        this.callbackId = 0;
    }

    call(method, params = {}) {
        return new Promise((resolve) => {
            this.callbackId++;

            let id = `call_${this.callbackId}`;
            this.callbacks[id] = resolve;

            window.external.notify(JSON.stringify({ id: id, method: method, parameters: params }));
        });
    }

    callback(id, data) {
        this.callbacks[id](data);
        delete this.callbacks[id];
    }
}

// need global function to call from native

const bridge = new Bridge();

function _bridgeCallback(id, data) {
    bridge.callback(id, data);
}