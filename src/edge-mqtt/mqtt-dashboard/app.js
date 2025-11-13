// ---- Config ----
const OFFLINE_AFTER_MS = 10_000;    // seen within last 10s = online
const CHECK_EVERY_MS   = 2_000;     // scan interval 2s

// ---- State ----
let client = null;
const DEVICES = new Map(); // deviceId -> { lastSeen:number, count:number }

// ---- Elements ----
const $ = (id) => document.getElementById(id);
const els = {
  url: $('url'), user: $('user'), pass: $('pass'), connect: $('connect'),
  autoTopic: $('autoTopic'), subBtn: $('subBtn'),
  status: $('status'), deviceGrid: $('deviceGrid'),
  console: $('console'),
  dbUrl: $('dbUrl'), runQuery: $('runQuery'), sql: $('sql'),
  saved: $('saved'), saveQuery: $('saveQuery'), deleteQuery: $('deleteQuery'),
  queryResults: $('queryResults'),
};

// ---- Console logger ----
const clog = (level, msg) => {
  const div = document.createElement('div');
  div.textContent = `[${new Date().toLocaleTimeString()}] ${msg}`;
  div.className = level === 'ok' ? 'ok' : level === 'warn' ? 'warn' : 'err';
  els.console.appendChild(div);
  els.console.scrollTop = els.console.scrollHeight;
};

// ---- Devices ----
function renderDevices() {
  const now = Date.now();
  const frag = document.createDocumentFragment();
  [...DEVICES.keys()].sort().forEach(id => {
    const { lastSeen, count } = DEVICES.get(id);
    const online = (now - lastSeen) <= OFFLINE_AFTER_MS;
    console.log(DEVICES.get(id));
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
  });
  els.deviceGrid.innerHTML = '';
  els.deviceGrid.appendChild(frag);
}

setInterval(renderDevices, CHECK_EVERY_MS);

function touchDevice(id) {
  const now = Date.now();
  const info = DEVICES.get(id) || { lastSeen: 0, count: 0 };
  info.lastSeen = now;
  info.count = (info.count || 0) + 1;
  DEVICES.set(id, info);
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
      client.subscribe('pico/+/status', { qos: 1 });
      clog('ok', 'Subscribed to pico/+/status');
      // and telemetry if set
      const topic = els.autoTopic.value.trim();
      if (topic) { client.subscribe(topic, { qos: 1 }); clog('ok', `Subscribed to ${topic}`); }
    });

    client.on('reconnect', () => { els.status.textContent='reconnecting…'; els.status.className='badge warn'; });
    client.on('close',     () => { els.status.textContent='disconnected'; els.status.className='badge'; });
    client.on('error', (err)=> { clog('err', 'Error: ' + (err?.message || err)); });

    client.on('message', (topic, message) => {
      const m = topic.match(/^pico\/([^/]+)\/(telemetry|status)$/);
      if (m) { touchDevice(m[1]); renderDevices(); }
    });
  } catch (e) { clog('err', 'JS error: ' + e.message); }
};

els.subBtn.onclick = () => {
  if (!client) return alert('Connect first');
  const topic = els.autoTopic.value.trim();
  if (!topic) return;
  client.subscribe(topic, { qos: 1 }, (err) => {
    if (err) clog('err', 'Subscribe failed: ' + err.message);
    else clog('ok', 'Subscribed to ' + topic);
  });
};

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
  for (const r of rows) html += '<tr>' + cols.map(c => `<td>${r[c] ?? ''}</td>`).join('') + '</tr>';
  html += '</tbody></table>';
  els.queryResults.innerHTML = html;
}


async function runQuery() {
  const api = els.dbUrl.value.trim();
  const sql = els.sql.value.trim();
  if (!api || !sql) return alert('Provide DB API URL and SQL');
  try {
    const res = await fetch(api, { method:'POST', headers:{'Content-Type':'application/json'}, body: JSON.stringify({ sql }) });
    if (!res.ok) throw new Error(`HTTP ${res.status} ${await res.text()}`);
    const data = await res.json();
    renderTable(data.rows || []);
    clog('ok', `Query OK: ${data.rows?.length ?? 0} rows`);
  } catch (e) { clog('err', 'Query failed: ' + e.message); }
}
els.runQuery.onclick = runQuery;

// saved queries
const LS_KEY = 'saved_sql_queries';
function loadSaved() {
  const arr = JSON.parse(localStorage.getItem(LS_KEY) || '[]');
  els.saved.innerHTML = '';
  arr.forEach((q,i)=>{ const o=document.createElement('option'); o.value=i; o.textContent=q.name||`Query ${i+1}`; els.saved.appendChild(o); });
  return arr;
}
function saveCurrent() {
  const name = prompt('Name this query:');
  const sql = els.sql.value.trim();
  if (!name || !sql) return;
  const arr = loadSaved(); arr.push({ name, sql }); localStorage.setItem(LS_KEY, JSON.stringify(arr)); loadSaved();
}
function deleteSelected() {
  const arr = loadSaved(); const idx = parseInt(els.saved.value,10);
  if (isNaN(idx)) return; arr.splice(idx,1); localStorage.setItem(LS_KEY, JSON.stringify(arr)); loadSaved();
}
els.saveQuery.onclick = saveCurrent;
els.deleteQuery.onclick = deleteSelected;
els.saved.onchange = ()=>{ const arr=loadSaved(); const idx=parseInt(els.saved.value,10); if(!isNaN(idx)) els.sql.value = arr[idx].sql; };

loadSaved();
renderDevices(); // initial
