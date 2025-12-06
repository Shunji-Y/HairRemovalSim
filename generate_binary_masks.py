"""
Generate 14 Binary Mask Textures for Per-Part Highlighting
Each texture is white (255) where the part is, black (0) elsewhere.

Usage:
    python generate_binary_masks.py
"""

from PIL import Image, ImageDraw
import os

TEXTURE_SIZE = 2048
OUTPUT_DIR = "Assets/Textures/PartMasks"

# UV Regions from BodyPartMaskGenerator log
# Format: (x, y, width, height)
PART_REGIONS = {
    # Head (Material 0)
    "Beard": [(0.25, 0.2, 0.5, 0.5)],
    
    # Body (Material 1)
    "Chest": [(0.28, 0.66, 0.42, 0.34)],
    "Abs": [(0.37, 0.26, 0.26, 0.38)],
    "Back": [(0.0, 0.2, 0.2, 0.8), (0.8, 0.2, 0.2, 0.8)],  # Two regions
    "LeftArmpit": [(0.7, 0.75, 0.082, 0.09)],
    "RightArmpit": [(0.199, 0.72, 0.082, 0.12)],
    
    # Arm (Material 2)
    "LeftUpperArm": [(0.651, 0.6, 0.349, 0.4)],
    "LeftLowerArm": [(0.5, 0.31, 0.5, 0.28)],
    "RightUpperArm": [(0.0, 0.6, 0.349, 0.4)],
    "RightLowerArm": [(0.0, 0.31, 0.5, 0.28)],
    
    # Leg (Material 3)
    "LeftThigh": [(0.5, 0.55, 0.5, 0.45)],
    "LeftCalf": [(0.5, 0.24, 0.5, 0.3)],
    "RightThigh": [(0.0, 0.55, 0.5, 0.45)],
    "RightCalf": [(0.0, 0.24, 0.5, 0.3)],
}

def generate_binary_mask(part_name, regions, size=TEXTURE_SIZE):
    """Generate a single binary mask texture"""
    # Create black image
    img = Image.new('RGB', (size, size), (0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    for (x, y, w, h) in regions:
        # Calculate end coordinates
        x2 = x + w
        y2 = y + h
        
        # Convert UV to pixel coordinates
        # UV: Y=0 is bottom, Y=1 is top
        # Image: Y=0 is top
        px1 = int(x * size)
        px2 = int(x2 * size)
        py1 = int((1.0 - y2) * size)  # Top of rect
        py2 = int((1.0 - y) * size)   # Bottom of rect
        
        # Ensure correct order
        px1, px2 = min(px1, px2), max(px1, px2)
        py1, py2 = min(py1, py2), max(py1, py2)
        
        # Paint white
        draw.rectangle([px1, py1, px2, py2], fill=(255, 255, 255))
        
        print(f"  Region: [{px1},{py1},{px2},{py2}]")
    
    # Save
    os.makedirs(OUTPUT_DIR, exist_ok=True)
    filename = f"Mask_{part_name}.png"
    filepath = os.path.join(OUTPUT_DIR, filename)
    img.save(filepath, 'PNG')
    print(f"  -> Saved: {filepath}")
    return img

def main():
    print("=" * 60)
    print("Binary Mask Texture Generator")
    print("=" * 60)
    print(f"Texture size: {TEXTURE_SIZE}x{TEXTURE_SIZE}")
    print(f"Output directory: {OUTPUT_DIR}")
    print()
    
    for part_name, regions in PART_REGIONS.items():
        print(f"[{part_name}]")
        generate_binary_mask(part_name, regions)
        print()
    
    print("=" * 60)
    print("Done! Import in Unity with these settings:")
    print("  - sRGB (Color Texture): OFF")
    print("  - Compression: None")
    print("  - Filter Mode: Point")
    print()
    print("Then assign each texture to the corresponding _Mask_{PartName} slot")
    print("in the materials.")
    print("=" * 60)

if __name__ == "__main__":
    main()
