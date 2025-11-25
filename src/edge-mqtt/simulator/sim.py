import os, time, json, random, socket, threading, queue
from datetime import datetime, timezone
import paho.mqtt.client as mqtt

# ---------- config ----------
BROKER_HOST = os.getenv("BROKER_HOST", "localhost")
BROKER_PORT = int(os.getenv("BROKER_PORT", "1883"))
BROKER_USER = os.getenv("BROKER_USER")
BROKER_PASS = os.getenv("BROKER_PASS")

MACHINE_TYPE = os.getenv("MACHINE_TYPE", "A").upper()  # "A" | "B" | "C" | "D"
HOSTNAME = socket.gethostname()
DEVICE_ID = os.getenv("DEVICE_ID", f"{MACHINE_TYPE}-{HOSTNAME[-6:]}")

# Topics
TOPIC_CMD       = f"iot.mqtt.cmd"                       # control for this machine
TOPIC_WORK_IN   = f"work/{DEVICE_ID}"                   # work assigned to this machine
TOPIC_HB        = f"hb/{DEVICE_ID}"                     # heartbeat
TOPIC_ACKS      = "handoff.acks"
TOPIC_REJECTS   = "rejects"
TOPIC_PROGRESS  = "progress"                            # progress & done events

# Behavior
HEARTBEAT_SEC   = int(os.getenv("HEARTBEAT_SEC", "5"))
TICK_MS_MIN     = int(os.getenv("TICK_MS_MIN", "250"))
TICK_MS_MAX     = int(os.getenv("TICK_MS_MAX", "750"))
# How many progress ticks per unit (simulates amount of work)
TICKS_PER_UNIT  = int(os.getenv("TICKS_PER_UNIT", "10"))

# ---------- process state ----------
operating = "idle"
work_q: "queue.Queue[dict]" = queue.Queue()

def now_iso():
    return datetime.now(timezone.utc).isoformat()

def publish_json(client, topic, payload, **kwargs):
    client.publish(topic, json.dumps(payload), **kwargs)

# ---------- MQTT callbacks ----------
def on_connect(client, userdata, flags, rc, properties=None):
    # Subscriptions
    client.subscribe(TOPIC_CMD, qos=1)
    client.subscribe(TOPIC_WORK_IN, qos=1)
    print(f"[{DEVICE_ID}] subscribing to: {TOPIC_CMD} and {TOPIC_WORK_IN}")

def on_message(client, userdata, msg):
    global operating
    try:
        payload = json.loads(msg.payload.decode("utf-8"))
    except Exception as e:
        print(f"[{DEVICE_ID}] invalid JSON on {msg.topic}: {e} => {msg.payload!r}")
        publish_json(client, TOPIC_REJECTS, {
            "ts": now_iso(), "device_id": DEVICE_ID,
            "reason": "invalid_json", "topic": msg.topic,
        }, qos=1)
        return

    if msg.topic == TOPIC_CMD:

        target = payload.get("device_id")
        if target != DEVICE_ID:
            print(f"[{DEVICE_ID}]", flush=True)
            return

       
        val = payload.get("value")
        if val == "idle":
            operating = "idle"
        elif val == "off":
            operating = "off"
        elif val == "running":
            operating = "running"
        else:
            print(f"[{DEVICE_ID}] unknown operating : {val}", flush=True)


    elif msg.topic == TOPIC_WORK_IN:
        print(f"[{DEVICE_ID}] work in", flush=True)
        # Expected work message shape (minimum):
        # {
        #   "unit_id": "...",
        #   "jobId": "...",
        #   "stage": "A|B|C|D",
        #   "assigned_machine_id": "<DEVICE_ID>",     # may be omitted if topic binding is per-device
        #   "assignment_version": <int>,
        #   "checkpoint": <int>,                    # optional
        #   "payload": {...}                        # domain content
        # }
        unit_id = payload.get("unit_id")
        assigned = payload.get("assigned_machine_id")
        version = payload.get("assignment_version")
        stage   = payload.get("stage")

        # Validate assignment if provided
        if assigned and assigned != DEVICE_ID:
            print(f"[{DEVICE_ID}] REJECT unit {unit_id}: assigned to {assigned}", flush=True)
            publish_json(client, TOPIC_REJECTS, {
                "ts": now_iso(), "unit_id": unit_id, "expected": DEVICE_ID,
                "seen_assigned": assigned, "stage": stage,
                "device_id": DEVICE_ID, "reason": "wrong_assignee",
                "seen_version": version
            }, qos=1)
            return

        # Enqueue for processing
        work_q.put(payload)
        # Send ACK immediately (handoff ack)
        publish_json(client, TOPIC_ACKS, {
            "ts": now_iso(), "unit_id": unit_id, "stage": stage,
            "device_id": DEVICE_ID, "assignment_version": version,
            "event": "handoff_ack"
        }, qos=1)
        print(f"[{DEVICE_ID}] ACK unit {unit_id} v{version}; queued", flush=True)

def heartbeat_thread(client: mqtt.Client, stop_evt: threading.Event):
    while not stop_evt.is_set():
        publish_json(client, TOPIC_HB, {
            "device_id": DEVICE_ID, "machine_type": MACHINE_TYPE,
            "ts": now_iso(), "status": operating
        }, qos=1)
        time.sleep(HEARTBEAT_SEC)

def worker_thread(client: mqtt.Client, stop_evt: threading.Event):
    global operating
    """Consume queued work and simulate processing with progress ticks."""
    while not stop_evt.is_set():
        try:
            item = work_q.get(timeout=0.2)
        except queue.Empty:
            continue

        unit_id = item.get("unit_id")
        version = item.get("assignment_version")
        stage   = item.get("stage")
        checkpoint = int(item.get("checkpoint") or 0)

        if operating != "running":
            # Requeue later if powered off
            print(f"[{DEVICE_ID}] NOT RUNNING", flush=True)
            work_q.put(item)
            time.sleep(0.5)
            continue

        print(f"[{DEVICE_ID}] START unit {unit_id} v{version} from checkpoint {checkpoint}", flush=True)
        publish_json(client, TOPIC_PROGRESS, {
            "ts": now_iso(), "unit_id": unit_id, "stage": stage,
            "machine_id": DEVICE_ID, "assignment_version": version,
            "event": "start", "checkpoint": checkpoint
        }, qos=1)

        # Simulate work
        current = checkpoint
        while current < TICKS_PER_UNIT and not stop_evt.is_set():
            # If machine turned off during work, pause:
            if operating != "running":
                print(f"[{DEVICE_ID}] PAUSED unit {unit_id} at tick {current}", flush=True)
                # put back with updated checkpoint
                item["checkpoint"] = current
                work_q.put(item)
                break

            # do one tick
            sleep_ms = random.randint(TICK_MS_MIN, TICK_MS_MAX)
            time.sleep(sleep_ms / 1000.0)
            current += 1
            publish_json(client, TOPIC_PROGRESS, {
                "ts": now_iso(), "unit_id": unit_id, "stage": stage,
                "machine_id": DEVICE_ID, "assignment_version": version,
                "event": "progress", "tick": current, "total": TICKS_PER_UNIT
            }, qos=0)

        if current >= TICKS_PER_UNIT:
            print(f"[{DEVICE_ID}] DONE unit {unit_id}", flush=True)
            publish_json(client, TOPIC_PROGRESS, {
                "ts": now_iso(), "unit_id": unit_id, "stage": stage,
                "machine_id": DEVICE_ID, "assignment_version": version,
                "event": "complete", "checkpoint": current
            }, qos=1)
            operating = "finish"

def main():
    stop_evt = threading.Event()
    client = mqtt.Client(client_id=DEVICE_ID, protocol=mqtt.MQTTv5)

    if BROKER_USER:
        client.username_pw_set(BROKER_USER, BROKER_PASS)

    client.on_connect = on_connect
    client.on_message = on_message

    client.connect(BROKER_HOST, BROKER_PORT, keepalive=30)
    client.loop_start()

    # start background threads
    hb_t = threading.Thread(target=heartbeat_thread, args=(client, stop_evt), daemon=True)
    wk_t = threading.Thread(target=worker_thread, args=(client, stop_evt), daemon=True)
    hb_t.start()
    wk_t.start()

    try:
        while True:
            time.sleep(1)
    except KeyboardInterrupt:
        pass
    finally:
        stop_evt.set()
        publish_json(client, TOPIC_HB, {
            "device_id": DEVICE_ID, "machine_type": MACHINE_TYPE,
            "ts": now_iso(), "status": operating
        }, qos=1)
        client.loop_stop()
        client.disconnect()

if __name__ == "__main__":
    main()
