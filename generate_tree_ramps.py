from PIL import Image, ImageDraw

def create_gradient(width, height, colors, filename):
    img = Image.new("RGB", (width, height), "#FFFFFF")
    draw = ImageDraw.Draw(img)
    
    for x in range(width):
        t = x / (width - 1)
        start_stop = colors[0]
        end_stop = colors[-1]
        
        for i in range(len(colors) - 1):
            if colors[i][0] <= t <= colors[i+1][0]:
                start_stop = colors[i]
                end_stop = colors[i+1]
                break
                
        if end_stop[0] == start_stop[0]:
            local_t = 0
        else:
            local_t = (t - start_stop[0]) / (end_stop[0] - start_stop[0])
            
        c1 = [int(start_stop[1][i:i+2], 16) for i in (1, 3, 5)]
        c2 = [int(end_stop[1][i:i+2], 16) for i in (1, 3, 5)]
        
        r = int(c1[0] + (c2[0] - c1[0]) * local_t)
        g = int(c1[1] + (c2[1] - c1[1]) * local_t)
        b = int(c1[2] + (c2[2] - c1[2]) * local_t)
        
        draw.line([(x, 0), (x, height)], fill=(r, g, b))
        
    img.save(filename)
    print(f"Created {filename}")

# Wood Body: Dark Brown -> Medium Brown -> Beige
wood_body = [
    (0.0, "#3E2723"), # Very Dark Brown
    (0.4, "#5D4037"), # Dark Brown
    (0.7, "#8D6E63"), # Medium Brown
    (1.0, "#D7CCC8")  # Light Beige Highlight
]

# Wood Edge: Very Dark Brown -> Transparent/White
wood_edge = [
    (0.0, "#281A16"), # Almost Black Brown
    (1.0, "#FFFFFF")
]

# Leaf Body: Dark Green -> Lush Green -> Yellowish Green
leaf_body = [
    (0.0, "#1B5E20"), # Deep Green
    (0.4, "#43A047"), # Mid Green
    (0.8, "#C5E1A5"), # Light Yellow-Green
    (1.0, "#F1F8E9")  # Highlight
]

# Leaf Edge: Dark Green
leaf_edge = [
    (0.0, "#0D3310"), # Very Dark Green
    (1.0, "#FFFFFF")
]

create_gradient(256, 4, wood_body, "Assets/_project/Textures/WoodBodyRamp.png")
create_gradient(256, 4, wood_edge, "Assets/_project/Textures/WoodEdgeRamp.png")
create_gradient(256, 4, leaf_body, "Assets/_project/Textures/LeafBodyRamp.png")
create_gradient(256, 4, leaf_edge, "Assets/_project/Textures/LeafEdgeRamp.png")
