
import os
import numpy as np
from PIL import Image

OUT_DIR = r"C:\Users\User\Documents\GitHub\hwing\Assets\_project\Textures\WatercolorRamps"
os.makedirs(OUT_DIR, exist_ok=True)

width, height = 512, 1

def save_ramp(name, data_func):
    # Generate 0..1 gradient
    x = np.linspace(0, 1, width)
    
    # Calculate RGB/Grayscale values
    pixels = data_func(x)
    
    # Convert to standard 0-255 uint8
    pixels = (np.clip(pixels, 0, 1) * 255).astype(np.uint8)
    
    # Reshape for Image (Height, Width, Channels)
    if pixels.ndim == 1:
        # Grayscale: (512,) -> (1, 512)
        pixels = np.tile(pixels, (height, 1))
        img = Image.fromarray(pixels, mode='L')
    else:
        # RGB: (512, 3) -> (1, 512, 3)
        pixels = np.tile(pixels, (height, 1, 1))
        img = Image.fromarray(pixels, mode='RGB')
        
    path = os.path.join(OUT_DIR, name)
    img.save(path)
    print(f"Saved {path}")

# 1. RampLightingA_Steps: 3-step lighting (Shadow, Mid, Highlight)
def ramp_a_steps(x):
    # 0.0 - 0.3: Shadow (0.1)
    # 0.3 - 0.7: Mid (0.5)
    # 0.7 - 1.0: Highlight (1.0)
    y = np.piecewise(x, 
        [x < 0.3, (x >= 0.3) & (x < 0.7), x >= 0.7],
        [0.2, 0.6, 1.0] 
    )
    return y

# 2. RampLightingB_Palette: Color Ramp (Warm/Cool)
def ramp_b_palette(x):
    # Shadow (0.0): Deep Cool Blue
    # Mid    (0.5): Neutral
    # Light  (1.0): Warm White
    r = np.interp(x, [0, 0.5, 1], [0.1, 0.8, 1.0])
    g = np.interp(x, [0, 0.5, 1], [0.1, 0.6, 0.95])
    b = np.interp(x, [0, 0.5, 1], [0.4, 0.5, 0.9])
    return np.dstack((r, g, b))[0]

# 3. RampEdgeA_Thin: Edge rim light mask
def ramp_edge_thin(x):
    return np.power(x, 8)

if __name__ == "__main__":
    print("Generating textures...")
    save_ramp("RampLightingA_Steps512x1.png", ramp_a_steps)
    save_ramp("RampLightingB_Palette512x1.png", ramp_b_palette)
    save_ramp("RampEdgeA_Thin512x1.png", ramp_edge_thin)
    print("Done.")
