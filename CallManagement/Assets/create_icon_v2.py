#!/usr/bin/env python3
"""
Create a professional Call Management app icon
"""

from PIL import Image, ImageDraw, ImageFilter, ImageFont
import os
import subprocess
import math

def create_rounded_rectangle(draw, coords, radius, fill):
    """Draw a rounded rectangle"""
    x1, y1, x2, y2 = coords
    
    # Draw the main rectangles
    draw.rectangle([x1 + radius, y1, x2 - radius, y2], fill=fill)
    draw.rectangle([x1, y1 + radius, x2, y2 - radius], fill=fill)
    
    # Draw the four corners
    draw.ellipse([x1, y1, x1 + 2*radius, y1 + 2*radius], fill=fill)
    draw.ellipse([x2 - 2*radius, y1, x2, y1 + 2*radius], fill=fill)
    draw.ellipse([x1, y2 - 2*radius, x1 + 2*radius, y2], fill=fill)
    draw.ellipse([x2 - 2*radius, y2 - 2*radius, x2, y2], fill=fill)

def create_gradient_background(size, margin, radius):
    """Create a gradient background image"""
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    
    # Create gradient
    for y in range(size):
        for x in range(size):
            # Check if inside rounded rect
            in_rect = True
            
            # Check corners
            corners = [
                (margin + radius, margin + radius),  # top-left
                (size - margin - radius, margin + radius),  # top-right
                (margin + radius, size - margin - radius),  # bottom-left
                (size - margin - radius, size - margin - radius),  # bottom-right
            ]
            
            if x < margin or x >= size - margin or y < margin or y >= size - margin:
                in_rect = False
            elif x < margin + radius and y < margin + radius:
                dist = math.sqrt((x - corners[0][0])**2 + (y - corners[0][1])**2)
                if dist > radius:
                    in_rect = False
            elif x >= size - margin - radius and y < margin + radius:
                dist = math.sqrt((x - corners[1][0])**2 + (y - corners[1][1])**2)
                if dist > radius:
                    in_rect = False
            elif x < margin + radius and y >= size - margin - radius:
                dist = math.sqrt((x - corners[2][0])**2 + (y - corners[2][1])**2)
                if dist > radius:
                    in_rect = False
            elif x >= size - margin - radius and y >= size - margin - radius:
                dist = math.sqrt((x - corners[3][0])**2 + (y - corners[3][1])**2)
                if dist > radius:
                    in_rect = False
            
            if in_rect:
                # Diagonal gradient
                progress = (x + y) / (2 * size)
                
                # Purple gradient: #6366F1 -> #8B5CF6 -> #A855F7
                if progress < 0.5:
                    p = progress * 2
                    r = int(99 + (139 - 99) * p)
                    g = int(102 + (92 - 102) * p)
                    b = int(241 + (246 - 241) * p)
                else:
                    p = (progress - 0.5) * 2
                    r = int(139 + (168 - 139) * p)
                    g = int(92 + (85 - 92) * p)
                    b = int(246 + (247 - 246) * p)
                
                img.putpixel((x, y), (r, g, b, 255))
    
    return img

def draw_phone_icon(img, size):
    """Draw a phone handset icon"""
    draw = ImageDraw.Draw(img)
    
    # Phone icon parameters
    cx, cy = size // 2, size // 2
    scale = size / 1024
    
    # Draw phone handset using bezier-like curves (simplified with ellipses and rectangles)
    white = (255, 255, 255, 255)
    white_semi = (255, 255, 255, 230)
    
    # Main phone body - rotated handset shape
    # We'll create it using multiple shapes
    
    # Handset - simplified curved shape
    handset_width = int(380 * scale)
    handset_height = int(380 * scale)
    
    # Create a separate image for the phone icon with rotation
    phone_img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    phone_draw = ImageDraw.Draw(phone_img)
    
    # Draw phone as connected ellipses and rectangles
    # Earpiece (top part)
    ear_x = cx - int(100 * scale)
    ear_y = cy - int(150 * scale)
    ear_w = int(120 * scale)
    ear_h = int(100 * scale)
    phone_draw.ellipse([ear_x, ear_y, ear_x + ear_w, ear_y + ear_h], fill=white)
    
    # Mouthpiece (bottom part)
    mouth_x = cx + int(20 * scale)
    mouth_y = cy + int(80 * scale)
    mouth_w = int(120 * scale)
    mouth_h = int(100 * scale)
    phone_draw.ellipse([mouth_x, mouth_y, mouth_x + mouth_w, mouth_y + mouth_h], fill=white)
    
    # Connecting bar
    bar_points = [
        (cx - int(40 * scale), cy - int(80 * scale)),
        (cx + int(80 * scale), cy + int(100 * scale)),
        (cx + int(120 * scale), cy + int(60 * scale)),
        (cx, cy - int(120 * scale)),
    ]
    phone_draw.polygon(bar_points, fill=white)
    
    # Additional fill for smooth connection
    phone_draw.ellipse([cx - int(60*scale), cy - int(60*scale), 
                        cx + int(100*scale), cy + int(100*scale)], fill=white)
    
    # Rotate the phone icon
    phone_img = phone_img.rotate(-30, center=(cx, cy), resample=Image.BICUBIC)
    
    # Composite onto main image
    img.paste(phone_img, (0, 0), phone_img)
    
    # Draw signal waves
    draw = ImageDraw.Draw(img)
    wave_cx = cx + int(140 * scale)
    wave_cy = cy - int(100 * scale)
    
    for i, r in enumerate([int(80*scale), int(140*scale), int(200*scale)]):
        # Draw arc (quarter circle)
        bbox = [wave_cx - r, wave_cy - r, wave_cx + r, wave_cy + r]
        draw.arc(bbox, start=180, end=270, fill=white_semi, width=int(28 * scale))
    
    # Small dot at the origin of waves
    dot_r = int(20 * scale)
    draw.ellipse([wave_cx - dot_r, wave_cy - dot_r, wave_cx + dot_r, wave_cy + dot_r], fill=white)
    
    return img

def create_icon():
    """Create the main icon"""
    size = 1024
    margin = 64
    radius = 180
    
    # Create gradient background
    print("Creating gradient background...")
    img = create_gradient_background(size, margin, radius)
    
    # Draw phone icon
    print("Drawing phone icon...")
    img = draw_phone_icon(img, size)
    
    # Apply slight blur for smoother edges
    # img = img.filter(ImageFilter.SMOOTH)
    
    return img

def create_iconset(img, output_dir):
    """Create all required sizes for macOS iconset"""
    iconset_dir = os.path.join(output_dir, "AppIcon.iconset")
    os.makedirs(iconset_dir, exist_ok=True)
    
    # Required sizes for macOS
    sizes = [
        (16, 1), (16, 2),
        (32, 1), (32, 2),
        (128, 1), (128, 2),
        (256, 1), (256, 2),
        (512, 1), (512, 2),
    ]
    
    for size, scale in sizes:
        actual_size = size * scale
        resized = img.resize((actual_size, actual_size), Image.LANCZOS)
        
        if scale == 1:
            filename = f"icon_{size}x{size}.png"
        else:
            filename = f"icon_{size}x{size}@2x.png"
        
        filepath = os.path.join(iconset_dir, filename)
        resized.save(filepath, "PNG")
        print(f"Created: {filename}")
    
    return iconset_dir

def main():
    script_dir = os.path.dirname(os.path.abspath(__file__))
    
    # Create the icon
    img = create_icon()
    
    # Save as PNG
    png_path = os.path.join(script_dir, "AppIcon_1024.png")
    img.save(png_path, "PNG")
    print(f"Saved: {png_path}")
    
    # Create iconset
    iconset_dir = create_iconset(img, script_dir)
    
    # Convert to icns using iconutil
    icns_path = os.path.join(script_dir, "AppIcon.icns")
    try:
        result = subprocess.run(
            ['iconutil', '-c', 'icns', iconset_dir, '-o', icns_path],
            capture_output=True, text=True
        )
        if result.returncode == 0:
            print(f"Created: {icns_path}")
        else:
            print(f"iconutil error: {result.stderr}")
    except FileNotFoundError:
        print("iconutil not found (not on macOS?)")
    
    # Also create ICO for Windows
    ico_path = os.path.join(script_dir, "AppIcon.ico")
    try:
        # Create multiple sizes for ICO
        ico_sizes = [16, 32, 48, 64, 128, 256]
        ico_images = [img.resize((s, s), Image.LANCZOS) for s in ico_sizes]
        ico_images[0].save(ico_path, format='ICO', sizes=[(s, s) for s in ico_sizes])
        print(f"Created: {ico_path}")
    except Exception as e:
        print(f"Error creating ICO: {e}")
    
    print("\n✅ Icon creation complete!")
    print(f"   PNG: {png_path}")
    print(f"   ICNS: {icns_path}")
    print(f"   ICO: {ico_path}")

if __name__ == "__main__":
    main()
