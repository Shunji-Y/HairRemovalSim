"""
Body Part Mask Texture Generator
Run this script from the project root directory.
It generates mask textures based on BodyPartDefinition settings.

Usage:
    python generate_masks.py

Requirements:
    pip install Pillow
"""

from PIL import Image, ImageDraw, ImageFont
import os

# Configuration
TEXTURE_SIZE = 2048
OUTPUT_DIR = "Assets/Textures/BodyPartMasks"

# Body Part Definitions (maskValue -> RGB)
# maskValue * 255 = R value (G=0, B=0)

def mask_to_rgb(mask_value):
    """Convert maskValue (0.0-1.0) to RGB tuple"""
    r = int(mask_value * 255)
    return (r, 0, 0)

# Define body parts for each material based on actual BodyPartDefinition uvRegions
# Format: (x_start, y_start, width, height, mask_value, name)
# These are EXACT values from the BodyPartMaskGenerator log

HEAD_PARTS = [
    # Beard: x=0.25, y=0.2, w=0.5, h=0.5
    (0.25, 0.2, 0.5, 0.5, 0.05, "Beard"),
]

BODY_PARTS = [
    # Chest: x=0.28, y=0.66, w=0.42, h=0.34
    (0.28, 0.66, 0.42, 0.34, 0.10, "Chest"),
    # Abs: x=0.37, y=0.26, w=0.26, h=0.38
    (0.37, 0.26, 0.26, 0.38, 0.15, "Abs"),
    # Back: x=0, y=0.2, w=0.2, h=0.8
    (0.0, 0.2, 0.2, 0.8, 0.20, "BackRight"),
    # Back: x=0.8, y=0.2, w=0.2, h=0.8
    (0.8, 0.2, 0.2, 0.8, 0.20, "BackLeft"),
    # LeftArmpit: x=0.7, y=0.75, w=0.082, h=0.09
    (0.7, 0.75, 0.082, 0.09, 0.35, "LeftArmpit"),
    # RightArmpit: x=0.199, y=0.72, w=0.082, h=0.12
    (0.199, 0.72, 0.082, 0.12, 0.50, "RightArmpit"),
]

ARM_PARTS = [
    # LeftUpperArm: x=0.651, y=0.6, w=0.349, h=0.4
    (0.651, 0.6, 0.349, 0.4, 0.25, "LeftUpperArm"),
    # LeftLowerArm: x=0.5, y=0.31, w=0.5, h=0.28
    (0.5, 0.31, 0.5, 0.28, 0.30, "LeftLowerArm"),
    # RightUpperArm: x=0, y=0.6, w=0.349, h=0.4
    (0.0, 0.6, 0.349, 0.4, 0.40, "RightUpperArm"),
    # RightLowerArm: x=0, y=0.31, w=0.5, h=0.28
    (0.0, 0.31, 0.5, 0.28, 0.45, "RightLowerArm"),
]

LEG_PARTS = [
    # LeftThigh: x=0.5, y=0.55, w=0.5, h=0.45
    (0.5, 0.55, 0.5, 0.45, 0.55, "LeftThigh"),
    # LeftCalf: x=0.5, y=0.24, w=0.5, h=0.3
    (0.5, 0.24, 0.5, 0.3, 0.60, "LeftCalf"),
    # RightThigh: x=0, y=0.55, w=0.5, h=0.45
    (0.0, 0.55, 0.5, 0.45, 0.65, "RightThigh"),
    # RightCalf: x=0, y=0.55, w=0.5, h=0.45 (assuming similar to LeftCalf position)
    (0.0, 0.24, 0.5, 0.3, 0.70, "RightCalf"),
]

def uv_to_pixel(uv_x, uv_y, size):
    """Convert UV coordinates to pixel coordinates"""
    # UV: Y=0 is bottom, Y=1 is top
    # Image: Y=0 is top, Y=height is bottom
    # So we need to flip Y
    px = int(uv_x * size)
    py = int((1.0 - uv_y) * size)  # Flip Y
    return px, py

def generate_mask(parts, filename, size=TEXTURE_SIZE):
    """Generate a mask texture for a set of body parts"""
    # Create black image
    img = Image.new('RGB', (size, size), (0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    # Format: (x, y, width, height, mask_value, name)
    for x, y, w, h, mask_value, name in parts:
        # Calculate end coordinates
        x2 = x + w
        y2 = y + h
        
        # Convert UV to pixel coordinates
        # UV: Y=0 is bottom, Y=1 is top
        # Image: Y=0 is top, Y=height is bottom
        px1 = int(x * size)
        px2 = int(x2 * size)
        py1 = int((1.0 - y2) * size)  # Top of rect (high UV Y = low pixel Y)
        py2 = int((1.0 - y) * size)   # Bottom of rect
        
        # Ensure correct order
        px1, px2 = min(px1, px2), max(px1, px2)
        py1, py2 = min(py1, py2), max(py1, py2)
        
        color = mask_to_rgb(mask_value)
        draw.rectangle([px1, py1, px2, py2], fill=color)
        
        print(f"  {name}: maskValue={mask_value}, R={color[0]}, rect=[{px1},{py1},{px2},{py2}]")
    
    # Save
    os.makedirs(OUTPUT_DIR, exist_ok=True)
    filepath = os.path.join(OUTPUT_DIR, filename)
    img.save(filepath, 'PNG')
    print(f"  -> Saved: {filepath}")
    return img

def main():
    print("=" * 60)
    print("Body Part Mask Texture Generator")
    print("=" * 60)
    print(f"Texture size: {TEXTURE_SIZE}x{TEXTURE_SIZE}")
    print(f"Output directory: {OUTPUT_DIR}")
    print()
    
    print("[Head]")
    generate_mask(HEAD_PARTS, "Head_BodyPartMask.png")
    print()
    
    print("[Body]")
    generate_mask(BODY_PARTS, "Body_BodyPartMask.png")
    print()
    
    print("[Arm]")
    generate_mask(ARM_PARTS, "Arm_BodyPartMask.png")
    print()
    
    print("[Leg]")
    generate_mask(LEG_PARTS, "Leg_BodyPartMask.png")
    print()
    
    print("=" * 60)
    print("Done! Import in Unity with these settings:")
    print("  - sRGB (Color Texture): OFF")
    print("  - Compression: None")
    print("  - Filter Mode: Point")
    print("=" * 60)
    
    # Also create a reference image showing all colors
    print()
    print("Creating color reference image...")
    ref_img = Image.new('RGB', (512, 768), (255, 255, 255))
    draw = ImageDraw.Draw(ref_img)
    
    all_parts = [
        ("Beard", 0.05),
        ("Chest", 0.10),
        ("Abs", 0.15),
        ("Back", 0.20),
        ("LeftUpperArm", 0.25),
        ("LeftLowerArm", 0.30),
        ("LeftArmpit", 0.35),
        ("RightUpperArm", 0.40),
        ("RightLowerArm", 0.45),
        ("RightArmpit", 0.50),
        ("LeftThigh", 0.55),
        ("LeftCalf", 0.60),
        ("RightThigh", 0.65),
        ("RightCalf", 0.70),
    ]
    
    y = 10
    for name, mask_value in all_parts:
        color = mask_to_rgb(mask_value)
        draw.rectangle([10, y, 60, y + 40], fill=color, outline=(128, 128, 128))
        draw.text((70, y + 10), f"{name}: {mask_value} (R={color[0]})", fill=(0, 0, 0))
        y += 50
    
    ref_path = os.path.join(OUTPUT_DIR, "ColorReference.png")
    ref_img.save(ref_path, 'PNG')
    print(f"  -> Saved: {ref_path}")

if __name__ == "__main__":
    main()
