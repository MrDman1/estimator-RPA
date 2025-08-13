from __future__ import annotations
import math


def poly_points(origin, segments):
    x, y = origin
    pts = [(x, y)]
    for s in segments:
        rad = math.radians(float(s["angle_deg"]))
        x += float(s["length_ft"]) * math.cos(rad)
        y += float(s["length_ft"]) * math.sin(rad)
        pts.append((x, y))
    return pts


def polygon_area(pts):
    s = 0.0
    for i in range(len(pts)):
        x1, y1 = pts[i]
        x2, y2 = pts[(i + 1) % len(pts)]
        s += x1 * y2 - x2 * y1
    return abs(s) / 2.0


def perimeter(pts):
    per = 0.0
    for i in range(len(pts) - 1):
        x1, y1 = pts[i]
        x2, y2 = pts[i + 1]
        per += ((x2 - x1) ** 2 + (y2 - y1) ** 2) ** 0.5
    # if closed shape, include return to origin
    if pts and pts[0] != pts[-1]:
        x1, y1 = pts[-1]
        x2, y2 = pts[0]
        per += ((x2 - x1) ** 2 + (y2 - y1) ** 2) ** 0.5
    return per


def subtract_openings(total_wall_len, openings_for_wall):
    return max(0.0, float(total_wall_len) - sum(float(o["width_ft"]) for o in openings_for_wall))


def close_is_return_to_origin(pts, tol=1e-6):
    if not pts:
        return False
    x1, y1 = pts[0]
    x2, y2 = pts[-1]
    return abs(x1 - x2) < tol and abs(y1 - y2) < tol


if __name__ == "__main__":
    segs = [
        dict(length_ft=120, angle_deg=0),
        dict(length_ft=80, angle_deg=90),
        dict(length_ft=120, angle_deg=180),
        dict(length_ft=80, angle_deg=270),
    ]
    pts = poly_points((0, 0), segs)
    a = polygon_area(pts)
    assert round(a, 5) == 9600.0
    print("geometry OK")
