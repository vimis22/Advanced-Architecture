// ---- Config ----
const OFFLINE_AFTER_MS = 6000;    // 3 heartbeat cycles = 3 seconds
const CHECK_EVERY_MS = 1000;       // scan interval 500ms

// ---- State ----
let client = null;
const MACHINES = new Map(); // machineId -> { type, status, unitId, progress, lastSeen, count }
let currentOrderId = null;

// ---- Elements ----
const $ = (id) => document.getElementById(id);
const els = {
  url: $('url'), user: $('user'), pass: $('pass'), connect: $('connect'),
  status: $('status'), machineGrid: $('machineGrid'),
  console: $('console'),
  // Order form
  orderTitle: $('orderTitle'),
  orderAuthor: $('orderAuthor'),
  orderPages: $('orderPages'),
  orderCoverType: $('orderCoverType'),
  orderPaperType: $('orderPaperType'),
  orderQuantity: $('orderQuantity'),
  createOrderBtn: $('createOrderBtn'),
  currentOrder: $('currentOrder'),
  // Queue status
  queueA: $('queueA'),
  queueB: $('queueB'),
  queueC: $('queueC'),
  queueD: $('queueD')
};

// ---- Console logger ----
const clog = (level, msg) => {
  const div = document.createElement('div');
  const timestamp = new Date().toLocaleTimeString();
  div.textContent = `[${timestamp}] ${msg}`;
  div.className = level === 'ok' ? 'ok' : level === 'warn' ? 'warn' : 'err';
  els.console.appendChild(div);
  els.console.scrollTop = els.console.scrollHeight;
};

// ---- Machine Rendering ----
function renderMachines() {
  const now = Date.now();
  const frag = document.createDocumentFragment();

  const sortedMachines = [...MACHINES.entries()].sort((a, b) => a[1].type.localeCompare(b[1].type));

  for (const [machineId, machine] of sortedMachines) {
    const { type, status, unitId, progress, lastSeen, count } = machine;
    const online = (now - lastSeen) <= OFFLINE_AFTER_MS;
    const secondsSinceHeartbeat = ((now - lastSeen) / 1000).toFixed(1);

    const card = document.createElement('div');
    card.className = 'machine-card';

    // Status indicator
    const statusDot = document.createElement('div');
    statusDot.className = `status-dot ${online ? 'online' : 'offline'}`;
    statusDot.title = online ? 'Online' : `Offline (${secondsSinceHeartbeat}s)`;

    // Machine info
    const header = document.createElement('div');
    header.className = 'machine-header';

    const typeLabel = document.createElement('div');
    typeLabel.className = 'machine-type';
    typeLabel.textContent = `Machine ${type}`;

    const machineIdLabel = document.createElement('div');
    machineIdLabel.className = 'machine-id';
    machineIdLabel.textContent = machineId;

    header.appendChild(statusDot);
    header.appendChild(typeLabel);
    header.appendChild(machineIdLabel);

    // Machine state
    const stateDiv = document.createElement('div');
    stateDiv.className = 'machine-state';

    const statusLabel = document.createElement('div');
    statusLabel.className = `machine-status status-${status}`;
    statusLabel.textContent = `Status: ${status.toUpperCase()}`;

    const unitLabel = document.createElement('div');
    unitLabel.className = 'machine-unit';
    unitLabel.textContent = `Unit: ${unitId || 'none'}`;

    const progressLabel = document.createElement('div');
    progressLabel.className = 'machine-progress';

    if (status === 'running' && progress !== null) {
      progressLabel.innerHTML = `
        <div class="progress-bar">
          <div class="progress-fill" style="width: ${progress}%"></div>
        </div>
        <span>${progress}%</span>
      `;
    } else {
      progressLabel.textContent = 'Progress: N/A';
    }

    const heartbeatLabel = document.createElement('div');
    heartbeatLabel.className = 'machine-heartbeat';
    heartbeatLabel.textContent = `Heartbeats: ${count} (last ${secondsSinceHeartbeat}s ago)`;

    stateDiv.appendChild(statusLabel);
    stateDiv.appendChild(unitLabel);
    stateDiv.appendChild(progressLabel);
    stateDiv.appendChild(heartbeatLabel);

    card.appendChild(header);
    card.appendChild(stateDiv);

    frag.appendChild(card);
  }

  els.machineGrid.innerHTML = '';
  els.machineGrid.appendChild(frag);
}

function renderMachinesLoop() {
  try {
    renderMachines();
  } catch (err) {
    console.error('renderMachines failed', err);
  } finally {
    setTimeout(renderMachinesLoop, CHECK_EVERY_MS);
  }
}

function updateMachine(heartbeat) {
  const { machine_id, machine_type, status, current_unit_id, progress, timestamp } = heartbeat;
  const now = Date.now();

  const existing = MACHINES.get(machine_id) || { count: 0 };
  MACHINES.set(machine_id, {
    type: machine_type,
    status: status,
    unitId: current_unit_id,
    progress: progress,
    lastSeen: now,
    count: existing.count + 1
  });
}

// ---- MQTT Connection ----
els.connect.onclick = () => {
  try {
    if (client) client.end(true);

    const url = els.url.value.trim();
    const username = els.user.value.trim();
    const password = els.pass.value;
    const opts = { clean: true };
    if (username) Object.assign(opts, { username, password });

    client = mqtt.connect(url, opts);
    els.status.textContent = 'connecting…';
    els.status.className = 'badge warn';
    clog('warn', `Connecting to ${url}...`);

    client.on('connect', () => {
      els.status.textContent = 'connected';
      els.status.className = 'badge ok';
      clog('ok', 'Connected to MQTT broker');

      // Subscribe to all machine heartbeats
      client.subscribe('machines/+/heartbeat', { qos: 1 }, (err) => {
        if (err) {
          clog('err', 'Failed to subscribe to heartbeats');
        } else {
          clog('ok', 'Subscribed to machines/+/heartbeat');
        }
      });

      // Subscribe to queue status updates
      client.subscribe('scheduler/queue/status', { qos: 1 }, (err) => {
        if (err) {
          clog('err', 'Failed to subscribe to queue status');
        } else {
          clog('ok', 'Subscribed to scheduler/queue/status');
        }
      });

      // Subscribe to order completion notifications
      client.subscribe('scheduler/order/completed', { qos: 1 }, (err) => {
        if (err) {
          clog('err', 'Failed to subscribe to order completed');
        } else {
          clog('ok', 'Subscribed to scheduler/order/completed');
        }
      });
    });

    client.on('reconnect', () => {
      els.status.textContent = 'reconnecting…';
      els.status.className = 'badge warn';
    });

    client.on('close', () => {
      els.status.textContent = 'disconnected';
      els.status.className = 'badge';
    });

    client.on('error', (err) => {
      clog('err', 'MQTT Error: ' + (err?.message || err));
    });

    client.on('message', (topic, message) => {
      try {
        const payload = message.toString();
        const data = JSON.parse(payload);

        if (topic.includes('/heartbeat')) {
          updateMachine(data);

          // Check for job completion
          if (data.progress === 100 && data.status === 'running') {
            //clog('ok', `✓ Job completed on ${data.machine_id}: unit ${data.current_unit_id}`);
          }

          // Check for machine going offline
          const machine = MACHINES.get(data.machine_id);
          if (machine && data.status === 'off') {
            //clog('warn', `⚠ Machine ${data.machine_id} went offline`);
          }
        } else if (topic === 'scheduler/queue/status') {
          // Update queue status display
          els.queueA.textContent = data.job_a || 0;
          els.queueB.textContent = data.job_b || 0;
          els.queueC.textContent = data.job_c || 0;
          els.queueD.textContent = data.job_d || 0;
        } else if (topic === 'scheduler/order/completed') {
          // Display order completion statistics
          clog('ok', data.message);
        }
      } catch (err) {
        console.error('Error processing message:', err);
      }
    });
  } catch (e) {
    clog('err', 'Connection error: ' + e.message);
  }
};

// ---- Create Order ----
els.createOrderBtn.onclick = () => {
  if (!client || !client.connected) {
    alert('Connect to MQTT broker first');
    return;
  }

  const title = els.orderTitle.value.trim();
  const author = els.orderAuthor.value.trim();
  const pages = parseInt(els.orderPages.value) || 200;
  const coverType = els.orderCoverType.value;
  const paperType = els.orderPaperType.value;
  const quantity = parseInt(els.orderQuantity.value) || 10;

  if (!title || !author) {
    alert('Please fill in title and author');
    return;
  }

  const order = {
    title,
    author,
    pages,
    cover_type: coverType,
    paper_type: paperType,
    quantity
  };

  // Publish order to scheduler (Note: The C# scheduler needs to subscribe to this topic)
  const orderTopic = 'scheduler/orders/create';
  client.publish(orderTopic, JSON.stringify(order), { qos: 1 }, (err) => {
    if (err) {
      clog('err', 'Failed to publish order: ' + err.message);
    } else {
      clog('ok', `Order created: "${title}" by ${author} (${quantity} units)`);
      els.currentOrder.innerHTML = `
        <strong>Current Order:</strong><br>
        Title: ${title}<br>
        Author: ${author}<br>
        Pages: ${pages}<br>
        Cover: ${coverType}, Paper: ${paperType}<br>
        Quantity: ${quantity} units
      `;
    }
  });
};

// ---- Machine Failure Detection ----
setInterval(() => {
  const now = Date.now();

  for (const [machineId, machine] of MACHINES.entries()) {
    const timeSinceHeartbeat = now - machine.lastSeen;

    // Alert if machine hasn't sent heartbeat in more than OFFLINE_AFTER_MS
    if (timeSinceHeartbeat > OFFLINE_AFTER_MS && timeSinceHeartbeat < OFFLINE_AFTER_MS + 1000) {
      // Only alert once (within 1 second window after timeout)
      if (machine.status !== 'off') {
        //clog('err', `🔴 MACHINE FAILURE: ${machineId} (Type ${machine.type}) - No heartbeat for ${(timeSinceHeartbeat / 1000).toFixed(1)}s`);
        if (machine.unitId) {
          //clog('warn', `   Unit ${machine.unitId} will be re-queued by scheduler`);
        }
      }
    }
  }
}, 1000);

// Start rendering loop
renderMachinesLoop();

clog('ok', 'Dashboard loaded. Connect to MQTT broker to begin.');
