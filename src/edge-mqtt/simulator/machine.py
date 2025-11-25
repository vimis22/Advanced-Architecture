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

# Topics
TOPIC_HB            = "hb"                       # heartbeat
TOPIC_CMD           = "cmd"                      # control for this machine
TOPIC_WORK_IN       = "work"                     # work assigned to this machine
TOPIC_ACK           = "ack"                      # acknowledgement (from this machine to schedule/machine)
TOPIC_PROGRESS      = "progress"                 # progress - used to monitor how far the machine is
TOPIC_ALERT         = "alert"                    # alert messages to the scheduler
TOPIC_MACHINE       = "new_machine"              # Assigning new next machine

# Behavior
HEARTBEAT_MS   = int(os.getenv("HEARTBEAT_MS", "1"))
TICK_MS_MIN     = int(os.getenv("TICK_MS_MIN", "250"))
TICK_MS_MAX     = int(os.getenv("TICK_MS_MAX", "750"))

cycle_min = TICK_MS_MIN
cycle_max = TICK_MS_MAX

# Monitoring and routing inforamtions
order_id = None
operation = "idle"  # machine status
unit_amount = 0     # order size
total_pages = 0     # amount of pages the book requires
next_machine = None # routing (device_id)
units_pending = 0   # amount of units the machine has in is inpur buffer
current = 0         # current produced
ack_receiver = None
ack_recived = False

# ---------- process state ----------
def now_iso():
    return datetime.now(timezone.utc).isoformat()

def publish_json(client, topic, payload, **kwargs):
    client.publish(topic, json.dumps(payload), **kwargs)

# ---------- MQTT callbacks ----------
def on_connect(client, userdata, flags, rc, properties=None):
    # Subscriptions
    client.subscribe(TOPIC_CMD, qos=1)
    client.subscribe(TOPIC_WORK_IN, qos=1)
    client.subscribe(TOPIC_ACK, qos=1)
    client.subscribe(TOPIC_PROGRESS, qos=1)
    client.subscribe(TOPIC_MACHINE, qos=1)
    print(f"[{DEVICE_ID}] subscribing to: {TOPIC_CMD}, {TOPIC_WORK_IN}, {TOPIC_ACK} and {TOPIC_MACHINE}", flush=True)

def on_message(client, userdata, msg):
    global order_id, operation, unit_amount, total_pages, next_machine, units_pending, current, cycle_min, cycle_max, ack_recived
    try:
        payload = json.loads(msg.payload.decode("utf-8"))
    except Exception as e:
        print(f"[{DEVICE_ID}] invalid JSON on {msg.topic}: {e} => {msg.payload!r}")
        return

    target = payload.get("device_id")
    if target != DEVICE_ID:
        #print(f"[{DEVICE_ID}] recieved {msg.topic}, {payload} ", flush=True)
        return

    sender = payload.get("from")

    if msg.topic == TOPIC_CMD:   
        val = payload.get("value")
        if val == "idle":
            operation = "idle"
        elif val == "off":
            operation = "off"
        elif val == "running":
            operation = "running"
            print(f"[{DEVICE_ID}] Starting again", flush=True)
        else:
            print(f"[{DEVICE_ID}] unknown operating : {val}", flush=True)

    elif msg.topic == TOPIC_WORK_IN:
        try:
            order_id = payload.get("order_id")
            unit_amount = int( payload.get("unit_amount") )
            total_pages = int( payload.get("total_pages") )
            next_machine = payload.get("next_machine")
            units_pending = int( payload.get("units_pending") )
            current = int( payload.get("units_produced") )
        except Exception as e:
            print(f"[{DEVICE_ID}] invalid order: {e} => {msg.payload!r}")
            return

        #{"device_id":"B-725c86","order_id":"1","unit_amount":"100","total_pages":"100","next_machine":"C-89e183","units_pending":"70","units_produced":"30","from":"scheduler"}
        # Send ACK immediately (ack)
        publish_json(client, TOPIC_ACK, {
            "device_id": sender,
            "ts": now_iso(),
            "event": "accepted",
            "from": DEVICE_ID
        }, qos=1)
        print(f"[{DEVICE_ID}] ACK sent to {sender}", flush=True)

    elif msg.topic == TOPIC_ACK:
        receiver = payload.get("from") 
        print(f"[{DEVICE_ID}] ACK received from {receiver}", flush=True)
        if ack_receiver == receiver:
            ack_recived = True
    elif msg.topic == TOPIC_PROGRESS:
        publish_json(client, TOPIC_ACK, {
            "device_id": sender,
            "ts": now_iso(),
            "event": "accepted",
            "from": DEVICE_ID
        }, qos=1)
        print(f"[{DEVICE_ID}] ACK sent to {sender}", flush=True)
        units_pending = units_pending + 1
    elif msg.topic == TOPIC_MACHINE:
        next_machine = payload.get("next_machine")
        print(f"[{DEVICE_ID}] received new next_machine {next_machine}", flush=True)
        publish_json(client, TOPIC_ACK, {
            "device_id": sender,
            "ts": now_iso(),
            "event": "accepted",
            "from": DEVICE_ID
        }, qos=1)


def heartbeat_thread(client: mqtt.Client, stop_evt: threading.Event):
    while not stop_evt.is_set():
        publish_json(client, TOPIC_HB, {
            "device_id": DEVICE_ID, "machine_type": MACHINE_TYPE,
            "ts": now_iso(), "status": operation
        }, qos=1)
        time.sleep(HEARTBEAT_MS)

def worker_thread(client: mqtt.Client, stop_evt: threading.Event):
    global operation, units_pending, current, next_machine, ack_receiver, ack_recived
    # simulate processing with progress ticks
    while not stop_evt.is_set():
        if operation != "running":
            # Requeue later if powered off
            # print(f"[{DEVICE_ID}] NOT RUNNING", flush=True)
            time.sleep(0.5)
            continue
    
        while units_pending > 0:
            # do one tick
            sleep_ms = random.randint(TICK_MS_MIN, TICK_MS_MAX)
            time.sleep(sleep_ms / 1000.0)

            current = current + 1
            units_pending = units_pending - 1
            publish_json(client, TOPIC_PROGRESS, {
                "device_id": next_machine, "order_id": order_id, "units_pending": units_pending,
                "current_produced" : current, "unit_amount": unit_amount, "from": DEVICE_ID
                }, qos=1)

            if next_machine != "null":
                ack_receiver = next_machine
                operation = "await"
                # waiting for ack
                wait_cycles = 0
                while ack_recived != True:
                    # if ack does not arrived. Alert scheduler and received a new next_machine
                    if wait_cycles >= 10:
                        publish_json(client, TOPIC_ALERT, {
                            "next_machine": next_machine, "from": DEVICE_ID
                            }, qos=1) 
                        next_machine = None
                        print(f"[{DEVICE_ID}] waiting for new Machine", flush=True)
                        while next_machine == None:
                            time.sleep(1)
                        publish_json(client, TOPIC_PROGRESS, {
                            "device_id": next_machine, "order_id": order_id, "units_pending": units_pending,
                            "current_produced" : current, "unit_amount": unit_amount, "from": DEVICE_ID
                            }, qos=1)
                        break
                    wait_cycles = wait_cycles + 1
                    time.sleep(0.1)
                print(f"[{DEVICE_ID}] Starting again", flush=True)
                ack_recived = False
                
            operation = "running"

            if current >= unit_amount:
                print(f"[{DEVICE_ID}] DONE unit {order_id}", flush=True)
                operation = "finish"
                break

def schedule_random_shutdown(min_delay=20, max_delay=120):
    delay = random.uniform(min_delay, max_delay)
    print(f"[{DEVICE_ID}] Starting up Again – will shut down in {delay:.2f}s", flush=True)

    def _shutdown():
        time.sleep(delay)
        print(f"[{DEVICE_ID}] Simulating failure: exiting now.", flush=True)
        os._exit(1)  # hard exit: no cleanup, no signal handling, process just dies

    t = threading.Thread(target=_shutdown, daemon=True)
    t.start()

def main():
    global operation

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

    schedule_random_shutdown(20, 120)

    try:
        while True:
            time.sleep(1)
    except KeyboardInterrupt:
        pass
    finally:
        stop_evt.set()
        operation = "off"
        publish_json(client, TOPIC_HB, {
            "device_id": DEVICE_ID, "machine_type": MACHINE_TYPE,
            "ts": now_iso(), "status": operation
        }, qos=1)
        client.loop_stop()
        client.disconnect()

if __name__ == "__main__":
    main()