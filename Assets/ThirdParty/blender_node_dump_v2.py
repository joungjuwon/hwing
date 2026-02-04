import bpy, json

# Blender node dump v2: includes socket indices, color ramps, and RGB curve data.
OUT_PATH = r"C:\Users\User\Documents\GitHub\hwing\Assets\ThirdParty\node_dump_v2.json"

try:
    from mathutils import Vector, Color, Euler, Quaternion, Matrix
except Exception:
    Vector = Color = Euler = Quaternion = Matrix = ()


def serialize(v):
    if v is None or isinstance(v, (int, float, str, bool)):
        return v
    if isinstance(v, (list, tuple)):
        return [serialize(x) for x in v]
    if isinstance(v, bpy.types.ID):
        return v.name
    if isinstance(v, (Vector, Color, Euler, Quaternion)):
        return [float(x) for x in v]
    if isinstance(v, Matrix):
        return [[float(x) for x in row] for row in v]
    try:
        return v.to_list()
    except Exception:
        pass
    if hasattr(v, "name"):
        return v.name
    return str(v)


def export_socket(s, index):
    info = {
        "index": index,
        "name": s.name,
        "identifier": s.identifier,
        "type": s.bl_idname,
        "is_linked": s.is_linked,
    }
    if hasattr(s, "default_value"):
        try:
            info["default"] = serialize(s.default_value)
        except Exception:
            pass
    if hasattr(s, "min_value"):
        info["min"] = s.min_value
    if hasattr(s, "max_value"):
        info["max"] = s.max_value
    return info


def export_color_ramp(n):
    cr = n.color_ramp
    ramp = {
        "interpolation": cr.interpolation,
        "hue_interpolation": getattr(cr, "hue_interpolation", None),
        "color_mode": getattr(cr, "color_mode", None),
        "elements": [],
    }
    for e in cr.elements:
        ramp["elements"].append(
            {
                "position": float(e.position),
                "color": [float(c) for c in e.color],
            }
        )
    return ramp


def export_rgb_curves(n):
    m = n.mapping
    data = {
        "use_clip": getattr(m, "use_clip", None),
        "clip_min": serialize(getattr(m, "clip_min", None)),
        "clip_max": serialize(getattr(m, "clip_max", None)),
        "curves": [],
    }
    for attr in ("black_level", "white_level", "brightness", "contrast", "saturation", "tone"):
        if hasattr(m, attr):
            data[attr] = serialize(getattr(m, attr))

    for i, c in enumerate(m.curves):
        curve = {"index": i, "points": []}
        for p in c.points:
            pt = {"location": serialize(p.location)}
            if hasattr(p, "handle_type"):
                pt["handle_type"] = p.handle_type
            if hasattr(p, "select"):
                pt["select"] = p.select
            curve["points"].append(pt)
        data["curves"].append(curve)
    return data


def export_value_node(n):
    # ShaderNodeValue stores the value on the output socket.
    val = None
    try:
        if n.outputs:
            val = serialize(n.outputs[0].default_value)
    except Exception:
        pass
    return {"value": val}


def export_node_props(n):
    props = {}
    for p in n.bl_rna.properties:
        if p.is_readonly:
            continue
        key = p.identifier
        if key in {"location", "width", "height", "select", "mute", "label", "parent"}:
            continue
        try:
            props[key] = serialize(getattr(n, key))
        except Exception:
            pass
    return props


def export_node_extras(n):
    extras = {}
    if n.bl_idname == "ShaderNodeValToRGB":
        extras["color_ramp"] = export_color_ramp(n)
    elif n.bl_idname == "ShaderNodeRGBCurve":
        extras["rgb_curves"] = export_rgb_curves(n)
    elif n.bl_idname == "ShaderNodeValue":
        extras.update(export_value_node(n))
    elif n.bl_idname == "ShaderNodeGroup":
        extras["node_tree"] = n.node_tree.name if n.node_tree else None
    elif n.bl_idname == "ShaderNodeTexImage":
        img = n.image
        extras["image_name"] = img.name if img else None
        extras["image_filepath"] = img.filepath if img else None
        extras["colorspace"] = img.colorspace_settings.name if img else None
    return extras


def export_tree(tree):
    nodes = []
    for n in tree.nodes:
        node_data = {
            "name": n.name,
            "type": n.bl_idname,
            "label": n.label,
            "location": serialize(n.location),
            "parent": n.parent.name if n.parent else None,
            "inputs": [export_socket(s, i) for i, s in enumerate(n.inputs)],
            "outputs": [export_socket(s, i) for i, s in enumerate(n.outputs)],
            "props": export_node_props(n),
            "extras": export_node_extras(n),
        }
        nodes.append(node_data)

    links = []
    for l in tree.links:
        try:
            from_index = list(l.from_node.outputs).index(l.from_socket)
        except Exception:
            from_index = None
        try:
            to_index = list(l.to_node.inputs).index(l.to_socket)
        except Exception:
            to_index = None
        links.append(
            {
                "from_node": l.from_node.name,
                "from_socket": l.from_socket.name,
                "from_socket_index": from_index,
                "from_socket_identifier": l.from_socket.identifier,
                "to_node": l.to_node.name,
                "to_socket": l.to_socket.name,
                "to_socket_index": to_index,
                "to_socket_identifier": l.to_socket.identifier,
            }
        )

    return {"name": tree.name, "type": tree.bl_idname, "nodes": nodes, "links": links}


data = {
    "meta": {"blender_version": bpy.app.version_string},
    "materials": [],
    "worlds": [],
    "node_groups": [],
}

for m in bpy.data.materials:
    if m.use_nodes and m.node_tree:
        data["materials"].append(export_tree(m.node_tree))

for w in bpy.data.worlds:
    if w.use_nodes and w.node_tree:
        data["worlds"].append(export_tree(w.node_tree))

for g in bpy.data.node_groups:
    data["node_groups"].append(export_tree(g))

with open(OUT_PATH, "w", encoding="utf-8") as f:
    json.dump(data, f, ensure_ascii=False, indent=2)

print("Exported:", OUT_PATH)
