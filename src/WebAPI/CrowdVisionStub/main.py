"""
Minimal FastAPI stub matching Greenlab CrowdCountClient contract.
Run: pip install fastapi uvicorn; uvicorn main:app --host 0.0.0.0 --port 8000
POST /api/v1/occupancy  JSON {"lab_code":"...","camera_url":null} -> {"count": int}
"""
from fastapi import FastAPI
from pydantic import BaseModel

app = FastAPI(title="Greenlab Crowd Vision Stub")


class OccupancyIn(BaseModel):
    lab_code: str | None = None
    camera_url: str | None = None


@app.post("/api/v1/occupancy")
def occupancy(body: OccupancyIn):
    # Replace with real CV pipeline (OpenCV/YOLO/etc.)
    return {"count": 1}
