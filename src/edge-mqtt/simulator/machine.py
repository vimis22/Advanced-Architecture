import os, time, json, random, socket, threading, signal
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

# Topics - SIMPLIFIED: Only heartbeat and work assignment
TOPIC_HEARTBEAT = f"machines/{DEVICE_ID}/heartbeat"
TOPIC_WORK = f"machines/{DEVICE_ID}/work"

# Behavior
HEARTBEAT_INTERVAL = int(os.getenv("HEARTBEAT_INTERVAL", "1"))  # seconds
TICK_MS_MIN = int(os.getenv("TICK_MS_MIN", "250"))
TICK_MS_MAX = int(os.getenv("TICK_MS_MAX", "750"))

# Machine state
status = "idle"  # idle, running, off
current_unit_id = None
current_progress = 0
order_data = None

# ---------- utility functions ----------
def now_iso():
    return datetime.now(timezone.utc).isoformat()

def publish_json(client, topic, payload, **kwargs):
    client.publish(topic, json.dumps(payload), **kwargs)

def publish_heartbeat(client):
    """Publish current machine state to heartbeat topic"""
    heartbeat = {
        "machine_id": DEVICE_ID,
        "machine_type": MACHINE_TYPE,
        "status": status,
        "current_unit_id": current_unit_id,
        "progress": current_progress if status == "running" else None,
        "timestamp": now_iso()
    }
    publish_json(client, TOPIC_HEARTBEAT, heartbeat, qos=1)

# ---------- MQTT callbacks ----------
def on_connect(client, userdata, flags, rc, properties=None):
    """Subscribe only to work assignment topic"""
    client.subscribe(TOPIC_WORK, qos=1)
    print(f"[{DEVICE_ID}] Connected to broker, subscribed to: {TOPIC_WORK}", flush=True)

    # Publish initial heartbeat
    publish_heartbeat(client)

def on_message(client, userdata, msg):
    """Handle work assignments from scheduler"""
    global status, current_unit_id, current_progress, order_data

    try:
        payload = json.loads(msg.payload.decode("utf-8"))
    except Exception as e:
        print(f"[{DEVICE_ID}] Invalid JSON on {msg.topic}: {e} => {msg.payload!r}", flush=True)
        return

    if msg.topic == TOPIC_WORK:
        # Scheduler assigned a unit to this machine
        # Expected payload:
        # {
        #   "unit_id": "unit-42",
        #   "order_data": {
        #     "title": "Book Title",
        #     "author": "Author Name",
        #     "pages": 200,
        #     "cover_type": "hardcover",
        #     "paper_type": "glossy"
        #   }
        # }

        if status != "idle":
            print(f"[{DEVICE_ID}] Received work but not idle (status={status}), ignoring", flush=True)
            return

        current_unit_id = payload.get("unit_id")
        order_data = payload.get("order_data", {})

        if not current_unit_id:
            print(f"[{DEVICE_ID}] Invalid work assignment (no unit_id): {payload}", flush=True)
            return

        # Start processing
        status = "running"
        current_progress = 0

        print(f"[{DEVICE_ID}] Received work assignment: unit {current_unit_id}", flush=True)
        print(f"[{DEVICE_ID}]   Order: {order_data.get('title', 'N/A')} by {order_data.get('author', 'N/A')}", flush=True)

        # Publish immediate heartbeat to confirm assignment
        publish_heartbeat(client)


def heartbeat_thread(client: mqtt.Client, stop_evt: threading.Event):
    """Continuously publish heartbeat every HEARTBEAT_INTERVAL seconds"""
    while not stop_evt.is_set():
        publish_heartbeat(client)
        time.sleep(HEARTBEAT_INTERVAL)

def worker_thread(client: mqtt.Client, stop_evt: threading.Event):
    """Simulate work processing and update progress"""
    global status, current_unit_id, current_progress, order_data

    while not stop_evt.is_set():
        if status != "running":
            # Not processing, wait a bit
            time.sleep(0.5)
            continue

        # Simulate work with random tick intervals
        sleep_ms = random.randint(TICK_MS_MIN, TICK_MS_MAX)
        time.sleep(sleep_ms / 1000.0)

        # Increment progress
        current_progress += random.randint(1, 5)

        if current_progress >= 100:
            current_progress = 100
            print(f"[{DEVICE_ID}] Completed unit {current_unit_id}", flush=True)

            # Publish final heartbeat with 100% progress
            publish_heartbeat(client)

            # Reset to idle after small delay
            time.sleep(0.5)
            status = "idle"
            current_unit_id = None
            current_progress = 0
            order_data = None

            print(f"[{DEVICE_ID}] Ready for next job", flush=True)

def schedule_random_shutdown(min_delay=20, max_delay=120):
    """Schedule a random machine failure for testing fault tolerance"""
    delay = random.uniform(min_delay, max_delay)
    print(f"[{DEVICE_ID}] Machine will simulate failure in {delay:.2f}s", flush=True)

    def _shutdown():
        global status
        time.sleep(delay)
        print(f"[{DEVICE_ID}] Simulating machine failure: shutting down", flush=True)
        status = "off"
        os._exit(1)  # Hard exit: simulates machine crash

    t = threading.Thread(target=_shutdown, daemon=True)
    t.start()

def main():
    global status

    stop_evt = threading.Event()
    client = mqtt.Client(client_id=DEVICE_ID, protocol=mqtt.MQTTv5)

    if BROKER_USER:
        client.username_pw_set(BROKER_USER, BROKER_PASS)

    client.on_connect = on_connect
    client.on_message = on_message

    print(f"[{DEVICE_ID}] Machine Type {MACHINE_TYPE} starting up...", flush=True)
    print(f"[{DEVICE_ID}] Connecting to MQTT broker at {BROKER_HOST}:{BROKER_PORT}", flush=True)

    client.connect(BROKER_HOST, BROKER_PORT, keepalive=30)
    client.loop_start()

    # Start background threads
    hb_t = threading.Thread(target=heartbeat_thread, args=(client, stop_evt), daemon=True)
    wk_t = threading.Thread(target=worker_thread, args=(client, stop_evt), daemon=True)
    hb_t.start()
    wk_t.start()

    # Optional: simulate random failures for testing
    # Uncomment to enable fault tolerance testing
    schedule_random_shutdown(60, 180)  # Fail between 60-180 seconds

    print(f"[{DEVICE_ID}] Machine ready and waiting for work", flush=True)

    try:
        while True:
            time.sleep(1)
    except KeyboardInterrupt:
        print(f"[{DEVICE_ID}] Shutting down gracefully...", flush=True)
    finally:
        stop_evt.set()
        status = "off"
        publish_heartbeat(client)
        time.sleep(0.5)  # Give time for final heartbeat to send
        client.loop_stop()
        client.disconnect()
        print(f"[{DEVICE_ID}] Shutdown complete", flush=True)

if __name__ == "__main__":
    main()