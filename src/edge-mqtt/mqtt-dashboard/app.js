// Guard: ensure mqtt.min.js is loaded
if (!window.mqtt) {
  alert('MQTT library not loaded. Ensure mqtt.min.js is included BEFORE app.js.');
  throw new Error('mqtt.min.js not loaded');
}

// --- Config ---
const OFFLINE_AFTER_MS = 100_000; // 10s silence => offline
const CHECK_EVERY_MS   = 20_000;  // scan interval
const ALERT_TOPIC      = "dashboard/alerts/offline";

// --- State ---
const DEVICES  = new Set(JSON.parse(localStorage.getItem("devices") || "[]"));
const LAST_SEEN = new Map();
let client = null;

// --- Elements ---
const $ = (id) => {
  const el = document.getElementById(id);
  if (!el) { console.error(`Missing #${id}`); alert(`Missing element id="${id}"`); throw new Error(`Missing #${id}`); }
  return el;
};
const els = {
  url: $('url'), user: $('user'), pass: $('pass'), connect: $('connect'),
  sub: $('sub'), subBtn: $('subBtn'),
  addDev: $('addDev'), addBtn: $('addBtn'),
  status: $('status'), deviceGrid: $('deviceGrid'), log: $('log'), console: $('console'),
};

// --- Helpers ---
const saveDevices = () => localStorage.setItem("devices", JSON.stringify([...DEVICES]));

const clog = (level, msg) => {
  const div = document.createElement('div');
  div.textContent = `[${new Date().toLocaleTimeString()}] ${msg}`;
  div.className = level === 'ok' ? 'ok' : level === 'warn' ? 'warn' : 'err';
  els.console.appendChild(div);
  els.console.scrollTop = els.console.scrollHeight;
};

const msglog = (topic, payload) => {
  const tr = document.createElement('tr');
  tr.innerHTML = `<td>${new Date().toLocaleTimeString()}</td><td>${topic}</td><td>${payload}</td>`;
  els.log.prepend(tr);
  if (els.log.rows.length > 500) els.log.deleteRow(500);
};

function renderDevices() {
  els.deviceGrid.innerHTML = "";
  [...DEVICES].sort().forEach(id => {
    const card = document.createElement('div');
    card.className = 'card';
    card.innerHTML = `
      <div class="id">${id}</div>
      <div class="btns">
        <button data-id="${id}" data-cmd="led_on">LED ON</button>
        <button data-id="${id}" data-cmd="led_off">LED OFF</button>
      </div>`;
    els.deviceGrid.appendChild(card);
  });
}

function publish(topic, payload) {
  if (!client || !client.connected) { clog('err', 'Not connected; publish skipped'); return; }
  const text = typeof payload === 'string' ? payload : JSON.stringify(payload);
  client.publish(topic, text, { qos: 1 });
  msglog('(publish)', `${topic} = ${text}`);
}

function publishCmd(id, cmd) { publish(`pico/${id}/cmd`, cmd); }

// Device buttons
els.deviceGrid.addEventListener('click', (e) => {
  const btn = e.target.closest('button'); if (!btn) return;
  const id = btn.dataset.id, cmd = btn.dataset.cmd;
  if (id && cmd) publishCmd(id, cmd);
});

// Add device
els.addBtn.onclick = () => {
  const id = els.addDev.value.trim();
  if (!id) return;
  DEVICES.add(id); saveDevices(); renderDevices();
  clog('ok', `Added device ${id}`); els.addDev.value = '';
};

// Connect
els.connect.onclick = () => {
  try {
    if (client) client.end(true);
    const url = els.url.value.trim();
    const username = els.user.value.trim();
    const password = els.pass.value;

    const opts = { clean: true };
    if (username) Object.assign(opts, { username, password });

    client = mqtt.connect(url, opts); // <— uses global 'mqtt'
    els.status.textContent = 'connecting…'; els.status.className = 'badge warn';
    clog('warn', `Connecting to ${url} …`);

    client.on('connect', () => {
      els.status.textContent = 'connected'; els.status.className = 'badge ok';
      clog('ok', 'Connected');
    });
    client.on('reconnect', () => {
      els.status.textContent = 'reconnecting…'; els.status.className = 'badge warn';
      clog('warn', 'Reconnecting…');
    });
    client.on('close', () => {
      els.status.textContent = 'disconnected'; els.status.className = 'badge';
      clog('err', 'Disconnected');
    });
    client.on('error', (err) => { clog('err', 'Error: ' + (err?.message || err)); });

    client.on('message', (topic, message) => {
      const text = message.toString();
      msglog(topic, text);
      // auto-discover + heartbeat
      const m = topic.match(/^pico\/([^/]+)\/(status|telemetry|cmd)$/);
      if (m) {
        const id = m[1];
        LAST_SEEN.set(id, Date.now());
        if (!DEVICES.has(id)) { DEVICES.add(id); saveDevices(); renderDevices(); clog('ok', `Auto-added ${id}`); }
      }
    });

    renderDevices();
  } catch (e) {
    clog('err', 'JS error: ' + e.message);
  }
};

// Subscribe
els.subBtn.onclick = () => {
  if (!client) return alert('Connect first');
  const topic = els.sub.value.trim() || '#';
  client.subscribe(topic, { qos: 1 }, (err) => {
    if (err) clog('err', 'Subscribe failed: ' + err.message);
    else clog('ok', 'Subscribed to ' + topic);
  });
  msglog('(subscribed)', topic);
};

// Heartbeat monitor
setInterval(() => {
  const now = Date.now();
  for (const id of [...DEVICES]) {
    const last = LAST_SEEN.get(id) || 0;
    if (now - last > OFFLINE_AFTER_MS) {
      publish(ALERT_TOPIC, { device_id: id, last_seen: new Date(last).toISOString() });
      DEVICES.delete(id); LAST_SEEN.delete(id); saveDevices(); renderDevices();
      msglog('(device removed)', id); clog('warn', `Removed offline device ${id}`);
    }
  }
}, CHECK_EVERY_MS);

// Initial render
renderDevices();
