-- TimescaleDB Initialization Script
-- This creates the schema for order storage and analytics
-- NOTE: Unit and machine real-time state is tracked in Redis, not here
-- This database only stores: orders, requeue events, and analytics

-- Enable TimescaleDB extension
CREATE EXTENSION IF NOT EXISTS timescaledb;

-- Orders table: stores complete order information
-- Order ID starts from 0 and auto-increments
CREATE TABLE IF NOT EXISTS orders (
    id SERIAL PRIMARY KEY,
    title TEXT NOT NULL,
    author TEXT NOT NULL,
    pages INTEGER NOT NULL,
    cover_type TEXT NOT NULL,
    paper_type TEXT NOT NULL,
    quantity INTEGER NOT NULL,
    status TEXT NOT NULL DEFAULT 'pending', -- pending, processing, completed, failed
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    started_at TIMESTAMPTZ, -- When first unit was assigned to a machine
    completed_at TIMESTAMPTZ,
    CONSTRAINT positive_quantity CHECK (quantity > 0),
    CONSTRAINT positive_pages CHECK (pages > 0)
);

-- Note: Hypertables disabled for simplicity. Enable for production with proper composite keys.
-- SELECT create_hypertable('orders', 'created_at', if_not_exists => TRUE);

-- Requeue events log: tracks when units are requeued due to machine failures
-- This is the PRIMARY PURPOSE of TimescaleDB - tracking requeue events for analytics
CREATE TABLE IF NOT EXISTS requeue_events (
    id BIGSERIAL PRIMARY KEY,
    unit_id TEXT NOT NULL,
    order_id INTEGER NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    job_type TEXT NOT NULL, -- job_a, job_b, job_c, job_d
    machine_id TEXT NOT NULL,
    machine_type TEXT NOT NULL,
    reason TEXT NOT NULL, -- 'machine_failure', 'timeout', 'orphaned'
    failure_detected_at TIMESTAMPTZ NOT NULL,
    unit_requeued_at TIMESTAMPTZ NOT NULL,
    recovery_duration_ms INTEGER NOT NULL,
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Note: Hypertables disabled for simplicity. Enable for production with proper composite keys.
-- SELECT create_hypertable('requeue_events', 'timestamp', if_not_exists => TRUE);

-- Create indexes for common queries
CREATE INDEX IF NOT EXISTS idx_orders_status ON orders(status);
CREATE INDEX IF NOT EXISTS idx_orders_created_at ON orders(created_at DESC);
CREATE INDEX IF NOT EXISTS idx_requeue_events_order_id ON requeue_events(order_id, timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_requeue_events_machine_id ON requeue_events(machine_id, timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_requeue_events_job_type ON requeue_events(job_type, timestamp DESC);

-- View: Requeue statistics for failure analysis
CREATE OR REPLACE VIEW requeue_statistics AS
SELECT
    r.order_id,
    r.job_type,
    r.machine_type,
    COUNT(*) as requeue_count,
    AVG(r.recovery_duration_ms) as avg_recovery_ms,
    MIN(r.recovery_duration_ms) as min_recovery_ms,
    MAX(r.recovery_duration_ms) as max_recovery_ms,
    COUNT(DISTINCT r.machine_id) as affected_machines
FROM requeue_events r
GROUP BY r.order_id, r.job_type, r.machine_type
ORDER BY requeue_count DESC;

-- View: Order duration statistics for performance analysis
CREATE OR REPLACE VIEW order_duration_statistics AS
SELECT
    o.id,
    o.title,
    o.quantity,
    o.created_at,
    o.started_at,
    o.completed_at,
    EXTRACT(EPOCH FROM (o.completed_at - o.started_at)) as duration_seconds,
    EXTRACT(EPOCH FROM (o.completed_at - o.started_at)) / 60 as duration_minutes,
    EXTRACT(EPOCH FROM (o.started_at - o.created_at)) as wait_time_seconds
FROM orders o
WHERE o.status = 'completed' AND o.completed_at IS NOT NULL AND o.started_at IS NOT NULL
ORDER BY o.created_at DESC;

-- Insert test data (optional)
-- INSERT INTO orders (title, author, pages, cover_type, paper_type, quantity)
-- VALUES ('The Art of Scheduling', 'Claude AI', 250, 'hardcover', 'glossy', 10);

GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO tsdbuser;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO tsdbuser;
