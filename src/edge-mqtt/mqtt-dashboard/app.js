// ---- Config ----
const OFFLINE_AFTER_MS = 2000;    // seen within last 2s = online
const CHECK_EVERY_MS = 500;     // scan interval 500ms

// ---- State ----
let client = null;
const DEVICES = new Map(); // deviceId -> { lastSeen:number, count:number }
const iterations = 100;
let iter = 0;
let done = false;
let rerouting = false;

// ---- Elements ----
const $ = (id) => document.getElementById(id);
const els = {
  url: $('url'), user: $('user'), pass: $('pass'), connect: $('connect'),
  autoTopic: $('autoTopic'), subBtn: $('subBtn'),
  pubTopic: $('pubTopic'), pubMessage: $('pubMessage'), pubBtn: $('pubBtn'),
  status: $('status'), deviceGrid: $('deviceGrid'),
  console: $('console'),
  dbUrl: $('dbUrl'), runQuery: $('runQuery'), sql: $('sql'),
  saved: $('saved'), saveQuery: $('saveQuery'), deleteQuery: $('deleteQuery'),
  queryResults: $('queryResults'),
};

// ---- Database ----
const api = "http://192.168.0.68:8090/query"

// ---- Console logger ----
const clog = (level, msg) => {
  const div = document.createElement('div');
  div.textContent = `[${new Date().toLocaleTimeString()}] ${msg}`;
  div.className = level === 'ok' ? 'ok' : level === 'warn' ? 'warn' : 'err';
  els.console.appendChild(div);
  els.console.scrollTop = els.console.scrollHeight;
};

// ---- Devices ----
async function renderDevices() {
  const now = Date.now();
  const frag = document.createDocumentFragment();

  for (const id of [...DEVICES.keys()].sort()) {
    const { lastSeen, count, status } = DEVICES.get(id);
    const online = (now - lastSeen) <= OFFLINE_AFTER_MS;

    if (!online && status == "running" && id[0] === 'A') {
      if (rerouting) return;
      rerouting = true;
      await Reroute(null, id);
    }

    const card = document.createElement('div');
    card.className = 'card';

    const left = document.createElement('div');
    left.className = 'left';

    const dot = document.createElement('div');
    dot.className = 'dot ' + (online ? 'on' : 'off');

    const name = document.createElement('div');
    name.className = 'name';
    name.textContent = id;

    left.appendChild(dot);
    left.appendChild(name);

    const right = document.createElement('div');
    right.className = 'counter';
    right.textContent = `msgs: ${count}`;

    card.appendChild(left);
    card.appendChild(right);

    frag.appendChild(card);
  }

  els.deviceGrid.innerHTML = '';
  els.deviceGrid.appendChild(frag);
}

async function renderDevicesLoop() {
  try {
    await renderDevices();         // wait until it's done
  } catch (err) {
    console.error('renderDevices failed', err);
  } finally {
    setTimeout(renderDevicesLoop, CHECK_EVERY_MS); // schedule next run
  }
}

function touchDevice(data) {
  const id = data.device_id
  const status = data.status
  const now = Date.now();
  const info = DEVICES.get(id) || { lastSeen: 0, count: 0, status: "idle" };
  info.lastSeen = now;
  info.count = (info.count || 0) + 1;
  info.status = status;
  DEVICES.set(id, info);
}

function ParseMessage(message) {
  let data;
  try {
    data = JSON.parse(message.toString());
    return data;
  } catch (err) {
    console.error('Bad JSON payload:', err, message.toString());
    return;
  }
}

// ---- MQTT ----
els.connect.onclick = () => {
  try {
    if (client) client.end(true);

    const url = els.url.value.trim();
    const username = els.user.value.trim();
    const password = els.pass.value;
    const opts = { clean: true };
    if (username) Object.assign(opts, { username, password });

    client = mqtt.connect(url, opts);
    els.status.textContent = 'connecting…'; els.status.className = 'badge warn';
    clog('warn', `Connecting to ${url} …`);

    client.on('connect', () => {
      els.status.textContent = 'connected'; els.status.className = 'badge ok';
      clog('ok', 'Connected');
      // always track status
      client.subscribe('cmd', { qos: 1 });
      client.subscribe('hb', { qos: 1 });
      client.subscribe('work', { qos: 1 });
      client.subscribe('ack', { qos: 1 });
      client.subscribe('progress', { qos: 1 });
      client.subscribe('alert', { qos: 1 });
      clog('ok', 'Subscribed to cmd, hb, work, ack, progress, alert');
      // and telemetry if set
      const topic = els.autoTopic.value.trim();
      if (topic) { client.subscribe(topic, { qos: 1 }); clog('ok', `Subscribed to ${topic}`); }
    });

    client.on('reconnect', () => { els.status.textContent = 'reconnecting…'; els.status.className = 'badge warn'; });
    client.on('close', () => { els.status.textContent = 'disconnected'; els.status.className = 'badge'; });
    client.on('error', (err) => { clog('err', 'Error: ' + (err?.message || err)); });

    client.on('message', async (topic, message) => {
      const hb = topic.match(/hb/);
      const alert = topic.match(/alert/);

      m = ParseMessage(message)

      if (hb) {
        touchDevice(m);
        renderDevices();

        if (m.device_id == endPoint && m.status == 'finish') {
          if (done) return;
          done = true;
          iter = iter + 1;
          if (iter >= 10) {
            clog('ok', 'Done')
            await SetAllMachineToIdle();
            return;
          }
          // example: start again with new parameters
          try {
            await StartProductionAgain(iter, 100, 100);
          } catch (e) {
            clog('err', 'Failed to restart production: ' + e.message);
          }

        }
      }
      else if (alert) {
        Reroute(m.from, m.next_machine);
      }
      else {
        //clog('ok', `Topic = ${topic}, message = ${message}`);
      }
    });
  } catch (e) { clog('err', 'JS error: ' + e.message); }
};

let endPoint = null

els.subBtn.onclick = () => {
  if (!client) return alert('Connect first');
  const topic = els.autoTopic.value.trim();
  if (!topic) return;
  client.subscribe(topic, { qos: 1 }, (err) => {
    if (err) clog('err', 'Subscribe failed: ' + err.message);
    else clog('ok', 'Subscribed to ' + topic);
  });
};

function publish(topic, payload) {
  //const text = typeof payload === 'string' ? payload : JSON.stringify(payload);
  client.publish(topic, payload, { qos: 1 });
  //clog('ok', `${topic} = ${payload}`);
}

function publishCmd(topic, cmd) { publish(topic, cmd); }

// Switch devices on/off
els.pubBtn.addEventListener('click', (e) => {
  const topic = els.pubTopic.value.trim();
  const message = els.pubMessage.value.trim();
  publishCmd(topic, message);
});

// ---- DB querying (read-only) ----
function renderTable(rows) {
  if (!els.queryResults) {
    console.warn('queryResults element not found');
    return;
  }
  if (!rows || rows.length === 0) {
    els.queryResults.innerHTML = '<div class="muted">No rows.</div>';
    return;
  }
  const cols = Object.keys(rows[0]);
  let html = '<table><thead><tr>' + cols.map(c => `<th>${c}</th>`).join('') + '</tr></thead><tbody>';
  for (const r of rows) {
    html += '<tr>' + cols.map(c => `<td>${r[c] ?? ''}</td>`).join('') + '</tr>';
  }
  html += '</tbody></table>';
  els.queryResults.innerHTML = html;
}

// #region DB

// ----- Get the devices from the database ------

async function Query(sql) {
  try {
    const res = await fetch(api, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ sql }) });
    if (!res.ok) throw new Error(`HTTP ${res.status} ${await res.text()}`);
    const data = await res.json();
    return data;
  } catch (e) { clog('err', 'Query failed: ' + e.message); }
}

async function QueryDevicesIdle() {
  const sql = `SELECT DISTINCT ON (machine_type) * FROM devices WHERE status = 'idle' ORDER BY machine_type, last_seen DESC`;
  const data = await Query(sql);
  return data;
}

async function DevicesToArray() {
  const query = await QueryDevicesIdle();
  const devices = query.rows.map(r => r.device_id);
  return devices;
}

async function SetAllMachineToIdle() {
  const query = await Query(`SELECT * FROM devices ORDER BY machine_type`);
  const devices = query.rows.map(r => r.device_id);

  for (let i = 0; i < devices.length; i++) { // set machine to running
    const payload = `{"device_id":"${devices[i]}","value":"idle","from":"scheduler"}`;
    publishCmd("cmd", payload);
  }
}

function SetMachineIdle(did) { publish("cmd", `{"device_id":"${did}","op":"power","value":"idle"}`); } //Set machine back to idle

els.saveQuery.addEventListener('click', async (e) => {
  e.preventDefault();
  await SetAllMachineToIdle();
});

async function StartProduction() {
  const devices = await DevicesToArray();
  let order = els.saved.value.trim();
  let order_items = order.split(",");
  if (order_items < 3) {
    return
  }

  for (let i = 0; i < devices.length; i++) { //send out order
    let next_machine = devices[i + 1];
    if (i == devices.length - 1) {
      next_machine = null;
      endPoint = devices[i];
    }

    const pending = i > 0 ? 0 : order_items[1];
    const payload = `{"device_id":"${devices[i]}","order_id":"${order_items[0]}","unit_amount":"${order_items[1]}","total_pages":"${order_items[2]}","next_machine":"${next_machine}","units_pending":"${pending}","units_produced":"0","from":"scheduler"}`;
    publishCmd("work", payload);
  }

  for (let i = 0; i < devices.length; i++) { // set machine to running
    const payload = `{"device_id":"${devices[i]}","value":"running","from":"scheduler"}`;
    publishCmd("cmd", payload);
  }

  clog('ok', "STARTING");
}

async function StartProductionAgain(id, amount, pages) {
  await SetAllMachineToIdle();

  const query = await Query(`SELECT DISTINCT ON (machine_type) * FROM devices ORDER BY machine_type, last_seen DESC`);
  const devices = query.rows.map(r => r.device_id);

  for (let i = 0; i < devices.length; i++) { //send out order
    let next_machine = devices[i + 1];
    if (i == devices.length - 1) {
      next_machine = null;
      endPoint = devices[i];
    }

    const pending = i > 0 ? 0 : amount
    const payload = `{"device_id":"${devices[i]}","order_id":"${id}","unit_amount":"${amount}","total_pages":"${pages}","next_machine":"${next_machine}","units_pending":"${pending}","units_produced":"0","from":"scheduler"}`;
    publishCmd("work", payload);
  }

  for (let i = 0; i < devices.length; i++) { // set machine to running
    const payload = `{"device_id":"${devices[i]}","value":"running","from":"scheduler"}`;
    publishCmd("cmd", payload);
  }

  clog('ok', "STARTING AGAIN");
  done = false;
}

function sleep(ms) {
  return new Promise(resolve => setTimeout(resolve, ms));
}

async function waitForIdleDevice(machineType, {
  delayMs = 1000,       // wait 1s between retries
  maxRetries = 30       // try up to 30 times (30 seconds total)
} = {}) {
  for (let attempt = 1; attempt <= maxRetries; attempt++) {
    const res = await Query(`
      SELECT device_id
      FROM devices
      WHERE machine_type='${machineType}'
        AND status='idle'
      ORDER BY last_seen DESC
      LIMIT 1
    `);

    if (res && res.rows && res.rows.length > 0) {
      const deviceId = res.rows[0].device_id;
      clog('ok', `Found idle device on attempt ${attempt}: ${deviceId}`);
      return deviceId;
    }

    clog('ok', `No idle device yet for ${machineType}, attempt ${attempt}`);

    // If not last attempt, wait before retrying
    if (attempt < maxRetries) {
      await sleep(delayMs);
    }
  }

  throw new Error(`No idle device found for machine_type=${machineType} after ${maxRetries} attempts`);
}

// #endregion

async function Reroute(alerting_device = null, stopped_device) {
  const startedAt = Date.now(); // start timing
  let successful = false;

  try {
    clog('ok', "ALERT!!! ");
    const stopped_device_data = await Query(
      `SELECT * FROM progress WHERE device_id='${stopped_device}'`);

    // find devices with the same machine_type
    let new_next_machine;
    try {
      new_next_machine = await waitForIdleDevice(stopped_device[0], {
        delayMs: 1000,   // retry every second
        maxRetries: 60   // try for up to 60 seconds
      });
    } catch (err) {
      clog('err', err.message);
      // publish timing inf
      return; // finally block will still run
    }

    endPoint = new_next_machine;

    const {
      device_id,
      target_id,
      order_id,
      units_pending,
      current_produced,
      unit_amount,
      updated_at
    } = stopped_device_data.rows[0];

    // Sending info to new machine and setting its status to running
    const payload_new = `{"device_id":"${new_next_machine}","order_id":"${order_id}","unit_amount":"${unit_amount}","total_pages":"100","next_machine":"${target_id}","units_pending":"${units_pending + 1}","units_produced":"${current_produced}","from":"scheduler"}`;
    const payload_start_new = `{"device_id":"${new_next_machine}","value":"running","from":"scheduler"}`;

    publishCmd("work", payload_new);
    publishCmd("cmd", payload_start_new);

    if (alerting_device !== null) {
      // Set new next machine for alerting device 
      const payload_alert = `{"device_id":"${alerting_device}","next_machine":"${new_next_machine}","from":"scheduler"}`;
      publishCmd("new_machine", payload_alert);
    }

    successful = true;

  } finally {
    const durationMs = Date.now() - startedAt;
    const payload_reroute = `{"order_id":"${iter}","reroute_time":"${durationMs}","successful":"${successful}"}`;

    // publish timing info
    publishCmd("reroute", payload_reroute);
    console.log("Reroute duration:", durationMs, "ms");
    rerouting = false;
  }
}


els.deleteQuery.onclick = StartProduction;

async function runQuery() {
  //const api = els.dbUrl.value.trim();
  const sql = els.sql.value.trim();
  if (!api || !sql) return alert('Provide DB API URL and SQL');
  try {
    const res = await fetch(api, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ sql }) });
    if (!res.ok) throw new Error(`HTTP ${res.status} ${await res.text()}`);
    const data = await res.json();
    renderTable(data.rows || []);
    clog('ok', `Query OK: ${data.rows?.length ?? 0} rows`);
  } catch (e) { clog('err', 'Query failed: ' + e.message); }
}
els.runQuery.onclick = runQuery;

async function sendCommand(deviceId, command) {
  const res = await fetch(`/api/devices/${deviceId}/cmd`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(command),
  });

  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Command failed: ${res.status} ${text}`);
  }
  return res.json();
}

renderDevicesLoop(); // start it once
