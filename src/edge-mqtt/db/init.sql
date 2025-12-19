DROP TABLE IF EXISTS telemetry_raw;
CREATE TABLE telemetry_raw (
  payload      text        NOT NULL,
  ingested_at  timestamptz NOT NULL DEFAULT now()
);

DROP TABLE IF EXISTS devices;
CREATE TABLE devices (
  device_id    text PRIMARY KEY,
  machine_type text,
  last_seen    timestamptz,
  status       text,
  updated_at   timestamptz
);

DROP TABLE IF EXISTS progress;
CREATE TABLE progress (
  device_id         text PRIMARY KEY,
  target_id         text,
  order_id          text,
  units_pending     text,
  current_produced  text,
  unit_amount       text,
  updated_at        timestamptz
);

DROP TABLE IF EXISTS reroutes;
CREATE TABLE reroutes (
  order_id     text PRIMARY KEY,
  reroute_time text,
  successful   text,
  updated_at   timestamptz
);

CREATE INDEX IF NOT EXISTS devices_type_idx ON devices(machine_type);

CREATE OR REPLACE FUNCTION telemetry_raw_to_devices()
RETURNS trigger
AS $fn$
DECLARE
  j    jsonb;
  did  text;
  mt   text;
  ts   timestamptz;
  st   text;
BEGIN
  -- Validate / parse JSON; ignore bad rows
  BEGIN
    j := NEW.payload::jsonb;
  EXCEPTION WHEN others THEN
    RETURN NEW;
  END;
  
  IF NOT (j ? 'status') THEN
    RETURN NEW;
  END IF;

  did := COALESCE(j->>'device_id', j->>'deviceId');
  mt  := COALESCE(j->>'machine_type', j->>'machineType');
  ts  := (COALESCE(j->>'ts', j->>'timestamp'))::timestamptz;
  st  := COALESCE(j->>'status', j->>'status');

  IF did IS NULL OR mt IS NULL OR ts IS NULL OR st IS NULL THEN
    RETURN NEW;
  END IF;

  INSERT INTO devices (device_id, machine_type, last_seen, status, updated_at)
  VALUES (did, mt, ts, st, now())
  ON CONFLICT (device_id) DO UPDATE
    SET machine_type = EXCLUDED.machine_type,
        last_seen    = GREATEST(devices.last_seen, EXCLUDED.last_seen),
        status       = EXCLUDED.status,
        updated_at   = now();

  RETURN NEW;
END;
$fn$ LANGUAGE plpgsql; 

DROP TRIGGER IF EXISTS trg_telemetry_raw_to_devices ON telemetry_raw;
CREATE TRIGGER trg_telemetry_raw_to_devices
AFTER INSERT ON telemetry_raw
FOR EACH ROW
EXECUTE FUNCTION telemetry_raw_to_devices();

CREATE OR REPLACE FUNCTION telemetry_raw_to_progress()
RETURNS trigger
AS $fn$
DECLARE
  j    jsonb;
  did  text;
  tid  text;
  oid  text;
  up   text;
  cp   text;
  ua   text;
BEGIN
  -- Validate / parse JSON; ignore bad rows
  BEGIN
    j := NEW.payload::jsonb;
  EXCEPTION WHEN others THEN
    RETURN NEW;
  END;
  
  -- Only handle rows that look like progress messages
  IF NOT (j ? 'units_pending') THEN
    RETURN NEW;
  END IF;

  -- Map JSON fields to variables (adjust keys to your actual payload)
  did := COALESCE(j->>'from', j->>'from');
  tid := COALESCE(j->>'device_id', j->>'deviceId');
  oid := COALESCE(j->>'order_id', j->>'orderId');
  up  := COALESCE(j->>'units_pending', j->>'unitsPending');
  cp  := COALESCE(j->>'current_produced', j->>'currentProduced');
  ua  := COALESCE(j->>'unit_amount', j->>'unitAmount');

  IF did IS NULL OR tid IS NULL OR oid IS NULL OR up IS NULL OR cp IS NULL OR ua IS NULL THEN
    RETURN NEW;
  END IF;

  INSERT INTO progress (device_id, target_id, order_id, units_pending, current_produced, unit_amount, updated_at)
  VALUES (did, tid, oid, up, cp, ua, now())
  ON CONFLICT (device_id) DO UPDATE
    SET target_id        = EXCLUDED.target_id,
        order_id         = EXCLUDED.order_id,
        units_pending    = EXCLUDED.units_pending,
        current_produced = EXCLUDED.current_produced,
        unit_amount      = EXCLUDED.unit_amount,
        updated_at       = now();

  RETURN NEW;
END;
$fn$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_telemetry_raw_to_progress ON telemetry_raw;
CREATE TRIGGER trg_telemetry_raw_to_progess
AFTER INSERT ON telemetry_raw
FOR EACH ROW
EXECUTE FUNCTION telemetry_raw_to_progress();

CREATE OR REPLACE FUNCTION telemetry_raw_to_reroutes()
RETURNS trigger
AS $fn$
DECLARE
  j   jsonb;
  oid text;
  rt  text;
  sf  text;
BEGIN
  -- Validate / parse JSON; ignore bad rows
  BEGIN
    j := NEW.payload::jsonb;
  EXCEPTION WHEN others THEN
    RETURN NEW;
  END;

  -- Only handle rows that look like reroute messages
  IF NOT (j ? 'reroute_time') THEN
    RETURN NEW;
  END IF;

  oid := COALESCE(j->>'order_id', j->>'orderId');
  rt  := j->>'reroute_time';
  sf  := j->>'successful';

  IF oid IS NULL OR rt IS NULL THEN
    RETURN NEW;
  END IF;

  INSERT INTO reroutes (order_id, reroute_time, successful, updated_at)
  VALUES (oid, rt, sf, now())
  ON CONFLICT (order_id) DO UPDATE
    SET reroute_time = EXCLUDED.reroute_time,
        successful   = EXCLUDED.successful,
        updated_at   = now();

  RETURN NEW;
END;
$fn$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_telemetry_raw_to_reroutes ON telemetry_raw;
CREATE TRIGGER trg_telemetry_raw_to_reroutes
AFTER INSERT ON telemetry_raw
FOR EACH ROW
EXECUTE FUNCTION telemetry_raw_to_reroutes();