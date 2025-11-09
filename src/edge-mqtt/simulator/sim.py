import os, time, json, random, socket
from datetime import datetime, timezone
import paho.mqtt.client as mqtt

# ---- config from env ----
BROKER_HOST = os.getenv("BROKER_HOST", "localhost")
BROKER_PORT = int(os.getenv("BROKER_PORT", "1883"))
BROKER_USER = os.getenv("BROKER_USER")
BROKER_PASS = os.getenv("BROKER_PASS")

PROFILE     = os.getenv("PROFILE", "normal")
BASE_TOPIC  = os.getenv("BASE_TOPIC", "pico")
TELEM_SFX   = os.getenv("TELEMETRY_TOPIC_SUFFIX", "telemetry")
STATUS_SFX  = os.getenv("STATUS_TOPIC_SUFFIX", "status")
CMD_SFX     = os.getenv("CMD_TOPIC_SUFFIX", "cmd")

INTERVAL_MS = int(os.getenv("INTERVAL_MS", "1000"))
JITTER_MS   = int(os.getenv("JITTER_MS", "200"))

# free-form extra data
EXTRA_TAGS  = os.getenv("EXTRA_TAGS", "")

# derive stable-enough device id from hostname
HOSTNAME = socket.gethostname()
DEVICE_ID = os.getenv("DEVICE_ID", f"{PROFILE}-{HOSTNAME[-6:]}")

TOPIC_TELEM  = f"{BASE_TOPIC}/{DEVICE_ID}/{TELEM_SFX}"
TOPIC_STATUS = f"{BASE_TOPIC}/{DEVICE_ID}/{STATUS_SFX}"
TOPIC_CMD    = f"{BASE_TOPIC}/{DEVICE_ID}/{CMD_SFX}"

seq = 0

def now_iso():
    return datetime.now(timezone.utc).isoformat()

def on_connect(client, userdata, flags, rc, properties=None):
    print(f"[{DEVICE_ID}] connected rc={rc}")
    # announce online
    client.publish(TOPIC_STATUS, json.dumps({
        "device_id": DEVICE_ID,
        "ts": now_iso(),
        "status": "online",
        "profile": PROFILE
    }), qos=1)
    # subscribe to commands
    client.subscribe(TOPIC_CMD, qos=1)

def on_message(client, userdata, msg):
    print(f"[{DEVICE_ID}] cmd {msg.topic} => {msg.payload!r}")

def build_payload():
    global seq
    seq += 1
    payload = {
        "device_id": DEVICE_ID,
        "ts": now_iso(),
        "seq": seq,
        "profile": PROFILE,
        "metrics": {
            "temp": round(20 + random.random() * 5, 2),
            "hum": round(40 + random.random() * 10, 2),
            "battery": round(3.7 - random.random() * 0.1, 3)
        }
    }
    # parse EXTRA_TAGS like "k=v,a=b"
    if EXTRA_TAGS:
        tags = {}
        for part in EXTRA_TAGS.split(","):
            if "=" in part:
                k, v = part.split("=", 1)
                tags[k.strip()] = v.strip()
        if tags:
            payload["tags"] = tags
    return payload

def main():
    client = mqtt.Client(client_id=DEVICE_ID, protocol=mqtt.MQTTv5)
    if BROKER_USER:
        client.username_pw_set(BROKER_USER, BROKER_PASS)
    client.on_connect = on_connect
    client.on_message = on_message

    client.connect(BROKER_HOST, BROKER_PORT, keepalive=30)
    client.loop_start()

    try:
        while True:
            payload = build_payload()
            client.publish(TOPIC_TELEM, json.dumps(payload), qos=1)
            print(f"[{DEVICE_ID}] -> {TOPIC_TELEM} {payload}")
            # base interval + jitter
            sleep_ms = INTERVAL_MS + random.randint(0, JITTER_MS)
            time.sleep(sleep_ms / 1000.0)
    except KeyboardInterrupt:
        pass
    finally:
        client.publish(TOPIC_STATUS, json.dumps({
            "device_id": DEVICE_ID,
            "ts": now_iso(),
            "status": "offline"
        }), qos=1)
        client.loop_stop()
        client.disconnect()

if __name__ == "__main__":
    main()
