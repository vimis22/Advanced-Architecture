-- migrations/001_init.sql

-- Enable UUID generation (Postgres extension)
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

CREATE TABLE IF NOT EXISTS machines (
  id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  name TEXT NOT NULL,
  type TEXT NOT NULL,             -- "printer","binder","cover","packager"
  pages_per_min INTEGER,          -- capacity for printers (nullable)
  status TEXT NOT NULL DEFAULT 'off',  -- 'off','idle','running'
  last_seen TIMESTAMP WITH TIME ZONE,
  metadata JSONB DEFAULT '{}'
);

CREATE TABLE IF NOT EXISTS books (
  id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  title TEXT NOT NULL,
  copies INTEGER NOT NULL DEFAULT 1,
  pages INTEGER NOT NULL,        -- pages per copy
  created_at TIMESTAMP WITH TIME ZONE DEFAULT now()
);

CREATE TABLE IF NOT EXISTS book_stages (
  id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  book_id UUID NOT NULL REFERENCES books(id) ON DELETE CASCADE,
  stage TEXT NOT NULL,           -- "printing", "cover", "binding", "packaging"
  status TEXT NOT NULL DEFAULT 'queued', -- queued,running,done,failed
  assigned_machine UUID REFERENCES machines(id),
  progress INTEGER NOT NULL DEFAULT 0, -- 0..100
  updated_at TIMESTAMP WITH TIME ZONE DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_book_stage_bookid_stage ON book_stages(book_id, stage);

CREATE TABLE IF NOT EXISTS job_events (
  id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
  book_stage_id UUID REFERENCES book_stages(id) ON DELETE CASCADE,
  machine_id UUID REFERENCES machines(id),
  event_type TEXT NOT NULL, -- "status","progress","error","ping"
  event_data JSONB,
  created_at TIMESTAMP WITH TIME ZONE DEFAULT now()
);
