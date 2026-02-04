import bpy, json

OUT_PATH = r"C:\Users\User\Documents\GitHub\hwing\Assets\ThirdParty\node_dump.json"

def serialize(v):
    if v is None or isinstance(v, (int, float, str, bool)):
        return v
    if hasattr(v, "to_list"):
        return v.to_list()
    if isinstance(v, (list, tuple)):
        return [serialize(x) for x in v]
    if isinstance(v, bpy.types.Image):
        return v.name
    if isinstance(v, bpy.types.NodeTree):
        return v.name
    if hasattr(v, "name"):
        return v.name
    return str(v)

def export_tree(tree):
    nodes = []
    for n in tree.nodes:
        props = {}
        for p in n.bl_rna.properties:
            if p.is_readonly:
                continue
            key = p.identifier
            if key in {"location", "width", "height", "select", "mute", "label"}:
                continue
            try:
                props[key] = serialize(getattr(n, key))
            except Exception:
                pass

        nodes.append({
            "name": n.name,
            "type": n.bl_idname,
            "location": serialize(n.location),
            "inputs": [
                {
                    "name": s.name,
                    "type": s.bl_idname,
                    "default": serialize(getattr(s, "default_value", None)),
                }
                for s in n.inputs
            ],
            "outputs": [{"name": s.name, "type": s.bl_idname} for s in n.outputs],
            "props": props,
        })

    links = []
    for l in tree.links:
        links.append(
            {
                "from_node": l.from_node.name,
                "from_socket": l.from_socket.name,
                "to_node": l.to_node.name,
                "to_socket": l.to_socket.name,
            }
        )

    return {"name": tree.name, "nodes": nodes, "links": links}

data = {"materials": [], "worlds": [], "node_groups": []}

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
