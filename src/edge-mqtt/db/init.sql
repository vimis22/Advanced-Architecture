CREATE EXTENSION IF NOT EXISTS timescaledb;
CREATE TABLE IF NOT EXISTS telemetry_raw (
  payload text NOT NULL
);

CREATE TABLE IF NOT EXISTS telemetry_flat (
  ts timestamptz NOT NULL,
  device_id text NOT NULL,
  seq integer NOT NULL,
  profile text,
  temp double precision,
  hum double precision,
  battery double precision,
  tags jsonb,
  PRIMARY KEY (device_id, ts, seq)
);

SELECT create_hypertable('telemetry_flat', 'ts', if_not_exists => TRUE);
CREATE INDEX IF NOT EXISTS telemetry_flat_ts_idx
  ON telemetry_flat (ts DESC);

CREATE INDEX IF NOT EXISTS telemetry_flat_dev_ts_idx
  ON telemetry_flat (device_id, ts DESC);

CREATE OR REPLACE FUNCTION telemetry_raw_to_flat()
RETURNS trigger AS $$
DECLARE
  j jsonb := NEW.payload::jsonb
BEGIN
  INSERT INTO telemetry_flat (
    ts, device_id, seq, profile,
    temp, hum, battery, tags
  )
  VALUES (
    (j ->> 'ts')::timestamptz,
    j ->> 'device_id',
    (j ->> 'seq')::integer,
    j->> 'profile',
    ((j -> 'metrics') ->> 'temp')::double precision,
    ((j -> 'metrics') ->> 'hum')::double precision,
    ((j -> 'metrics') ->> 'battery')::double precision,
    j -> 'tags'
  )
  ON CONFLICT (device_id, ts, seq) DO NOTHING;

  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_telemetry_raw_to_flat ON telemetry_raw;

CREATE TRIGGER trg_telemetry_raw_to_flat
AFTER INSERT ON telemetry_raw
FOR EACH ROW
EXECUTE FUNCTION telemetry_raw_to_flat();
DECLARE
  j jsonb := NEW.payload::jsonb
  INSERT INTO telemetry_flat (
    ts, device_id, seq, profile,
    temp, hum, battery, tags
  )
  VALUES (
    (payload::jsonb ->> 'ts')::timestamptz,
    payload::jsonb ->> 'device_id',
    (payload::jsonb ->> 'seq')::integer,
    payload::jsonb->> 'profile',
    ((payload::jsonb -> 'metrics') ->> 'temp')::double precision,
    ((payload::jsonb -> 'metrics') ->> 'hum')::double precision,
    ((payload::jsonb -> 'metrics') ->> 'battery')::double precision,
    payload::jsonb -> 'tags'
  )
  ON CONFLICT (device_id, ts, seq) DO NOTHING;
