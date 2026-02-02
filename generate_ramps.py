from PIL import Image, ImageDraw

def create_gradient(width, height, colors, filename):
    img = Image.new("RGB", (width, height), "#FFFFFF")
    draw = ImageDraw.Draw(img)
    
    # Simple linear interpolation for multiple stops
    # colors is a list of (position_0to1, hex_color)
    # e.g. [(0.0, "#000000"), (1.0, "#FFFFFF")]
    
    for x in range(width):
        t = x / (width - 1)
        
        # Find the two color stops t is between
        start_stop = colors[0]
        end_stop = colors[-1]
        
        for i in range(len(colors) - 1):
            if colors[i][0] <= t <= colors[i+1][0]:
                start_stop = colors[i]
                end_stop = colors[i+1]
                break
                
        # Remap t to 0..1 range between these stops
        if end_stop[0] == start_stop[0]:
            local_t = 0
        else:
            local_t = (t - start_stop[0]) / (end_stop[0] - start_stop[0])
            
        # Interpolate color
        c1 = [int(start_stop[1][i:i+2], 16) for i in (1, 3, 5)]
        c2 = [int(end_stop[1][i:i+2], 16) for i in (1, 3, 5)]
        
        r = int(c1[0] + (c2[0] - c1[0]) * local_t)
        g = int(c1[1] + (c2[1] - c1[1]) * local_t)
        b = int(c1[2] + (c2[2] - c1[2]) * local_t)
        
        draw.line([(x, 0), (x, height)], fill=(r, g, b))
        
    img.save(filename)
    print(f"Created {filename}")

# Rose Body Ramp: Dark Burgundy -> Soft Pink -> White
# Left (Dark/Shadow) -> Right (Light)
body_colors = [
    (0.0, "#501030"), # Deep Shadow
    (0.4, "#9B3E72"), # Midtone (Rose)
    (0.7, "#D5A781"), # Light warm pink
    (1.0, "#FFFFFF")  # Highlight
]

# Edge Ramp: Reddish Pink -> White
edge_colors = [
    (0.0, "#D04060"), # Dark Edge
    (1.0, "#FFFFFF")  # Fade out
]

create_gradient(256, 4, body_colors, "Assets/_project/Textures/RoseBodyRamp.png")
create_gradient(256, 4, edge_colors, "Assets/_project/Textures/RoseEdgeRamp.png")
