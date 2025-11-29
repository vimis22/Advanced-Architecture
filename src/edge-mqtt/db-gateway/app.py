from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
import os, psycopg2, psycopg2.extras

app = FastAPI(title="DB Gateway", version="1.1")

# Allow your dashboard origin(s)
app.add_middleware(
    CORSMiddleware,
    allow_origins=[
        #"http://192.168.0.68:8081",
        #"http://192.168.1.68:8081",
        "http://10.126.128.148:8081",
        "http://localhost:8081",
        "*",  # dev-friendly; tighten later
    ],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

class QueryIn(BaseModel):
    sql: str

def get_db_params():
    """Read DB connection params from env each request."""
    return dict(
        host=os.getenv("DB_HOST", "timescaledb"),
        port=int(os.getenv("DB_PORT", "5432")),
        user=os.getenv("DB_USER", "tsdbuser"),
        password=os.getenv("DB_PASS", "tsdbpass"),
        dbname=os.getenv("DB_NAME", "grid"),
    )

def is_safe(sql: str) -> str:
    """
    Return a sanitized SQL string if it's a single SELECT.
    Allows a single trailing semicolon.
    """
    if sql is None:
        return ""
    s = sql.strip()
    if s.endswith(";"):
        s = s[:-1].strip()
    low = s.lower()
    if not low.startswith("select"):
        return ""
    # block writes/DDL and multi-statements (crude, OK for a lab)
    banned = [" insert ", " update ", " delete ", " drop ", " alter ", " create ", " grant ", " revoke ", "--", "/*", "*/"]
    low_sp = f" {low} "
    if any(b in low_sp for b in banned):
        return ""
    return s

@app.post("/query")
def query(q: QueryIn):
    sql = is_safe(q.sql)
    if not sql:
        raise HTTPException(status_code=400, detail="Only single SELECT statements are allowed.")
    try:
        with psycopg2.connect(**get_db_params()) as conn:
            with conn.cursor(cursor_factory=psycopg2.extras.RealDictCursor) as cur:
                cur.execute(sql)
                rows = cur.fetchall() if cur.description else []
                # Convert RealDictRow -> plain dict list
                return {"rows": [dict(r) for r in rows]}
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))