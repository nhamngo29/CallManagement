#!/usr/bin/env python3
"""
Script to create Call Management app icon
Creates a modern phone/call icon with gradient background
"""

import subprocess
import os

# Check if PIL is available, if not use a simple SVG approach
try:
    from PIL import Image, ImageDraw
    HAS_PIL = True
except ImportError:
    HAS_PIL = False
    print("PIL not found, will create SVG icon instead")

def create_svg_icon():
    """Create an SVG icon for the app"""
    svg_content = '''<?xml version="1.0" encoding="UTF-8"?>
<svg width="1024" height="1024" viewBox="0 0 1024 1024" xmlns="http://www.w3.org/2000/svg">
  <defs>
    <!-- Gradient background -->
    <linearGradient id="bgGradient" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" style="stop-color:#4F46E5"/>
      <stop offset="100%" style="stop-color:#7C3AED"/>
    </linearGradient>
    <!-- Shadow -->
    <filter id="shadow" x="-20%" y="-20%" width="140%" height="140%">
      <feDropShadow dx="0" dy="8" stdDeviation="20" flood-color="#000" flood-opacity="0.3"/>
    </filter>
  </defs>
  
  <!-- Background rounded square -->
  <rect x="64" y="64" width="896" height="896" rx="200" ry="200" 
        fill="url(#bgGradient)" filter="url(#shadow)"/>
  
  <!-- Phone icon -->
  <g transform="translate(512, 512)">
    <!-- Phone handset -->
    <path d="M-280,-120 
             C-280,-180 -240,-220 -180,-220 
             L-100,-220 
             C-60,-220 -40,-200 -40,-160 
             L-40,-80 
             C-40,-40 -60,-20 -100,-20 
             L-140,-20
             C-140,60 -60,140 20,140
             L20,100
             C20,60 40,40 80,40
             L160,40
             C200,40 220,60 220,100
             L220,180
             C220,240 180,280 120,280
             C-120,280 -280,120 -280,-120 Z"
          fill="white"
          transform="rotate(-135) scale(0.9)"
          opacity="0.95"/>
    
    <!-- Signal waves -->
    <g stroke="white" stroke-width="24" fill="none" opacity="0.8" stroke-linecap="round">
      <path d="M80,-180 Q200,-180 200,-60" />
      <path d="M80,-260 Q280,-260 280,-60" />
      <path d="M80,-340 Q360,-340 360,-60" />
    </g>
  </g>
</svg>'''
    return svg_content

def create_simple_svg_icon():
    """Create a simpler, cleaner SVG icon"""
    svg_content = '''<?xml version="1.0" encoding="UTF-8"?>
<svg width="1024" height="1024" viewBox="0 0 1024 1024" xmlns="http://www.w3.org/2000/svg">
  <defs>
    <linearGradient id="bgGradient" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" style="stop-color:#6366F1"/>
      <stop offset="50%" style="stop-color:#8B5CF6"/>
      <stop offset="100%" style="stop-color:#A855F7"/>
    </linearGradient>
  </defs>
  
  <!-- Background -->
  <rect x="64" y="64" width="896" height="896" rx="180" ry="180" fill="url(#bgGradient)"/>
  
  <!-- Phone icon - Material Design style -->
  <g fill="white">
    <!-- Phone handset -->
    <path d="M744.4 560.4c-30.8-30.8-69.6-30.8-100.4 0l-52.8 52.8c-4.4 4.4-10.8 5.6-16.4 3.2-44-19.2-84.8-46.4-120.8-82.4s-63.2-76.8-82.4-120.8c-2.4-5.6-1.2-12 3.2-16.4l52.8-52.8c30.8-30.8 30.8-69.6 0-100.4l-70.4-70.4c-30.8-30.8-69.6-30.8-100.4 0l-37.6 37.6c-40 40-56 96-43.2 150.4 26.4 112 100 220 200.4 320.4s208.4 174 320.4 200.4c54.4 12.8 110.4-3.2 150.4-43.2l37.6-37.6c30.8-30.8 30.8-69.6 0-100.4l-70.4-70.4z"/>
    
    <!-- Signal waves -->
    <path d="M600 224c88 0 168 36 226 94s94 138 94 226" stroke="white" stroke-width="48" fill="none" stroke-linecap="round"/>
    <path d="M600 344c56 0 108 24 146 62s62 90 62 146" stroke="white" stroke-width="48" fill="none" stroke-linecap="round"/>
    <circle cx="600" cy="464" r="32" fill="white"/>
  </g>
</svg>'''
    return svg_content

def main():
    script_dir = os.path.dirname(os.path.abspath(__file__))
    
    # Create SVG file
    svg_path = os.path.join(script_dir, "AppIcon.svg")
    with open(svg_path, 'w') as f:
        f.write(create_simple_svg_icon())
    print(f"Created: {svg_path}")
    
    # Check if we can convert to PNG using sips or other tools
    iconset_dir = os.path.join(script_dir, "AppIcon.iconset")
    os.makedirs(iconset_dir, exist_ok=True)
    
    # Required sizes for macOS iconset
    sizes = [16, 32, 64, 128, 256, 512, 1024]
    
    # Try to use rsvg-convert or other SVG to PNG converter
    try:
        # First create a 1024x1024 PNG
        png_1024 = os.path.join(script_dir, "AppIcon_1024.png")
        
        # Try different conversion methods
        converted = False
        
        # Method 1: Try rsvg-convert (if available)
        try:
            subprocess.run(['rsvg-convert', '-w', '1024', '-h', '1024', svg_path, '-o', png_1024], 
                         check=True, capture_output=True)
            converted = True
            print("Converted using rsvg-convert")
        except (subprocess.CalledProcessError, FileNotFoundError):
            pass
        
        # Method 2: Try qlmanage (macOS built-in)
        if not converted:
            try:
                subprocess.run(['qlmanage', '-t', '-s', '1024', '-o', script_dir, svg_path],
                             check=True, capture_output=True)
                # qlmanage creates file with different name
                ql_output = svg_path + ".png"
                if os.path.exists(ql_output):
                    os.rename(ql_output, png_1024)
                    converted = True
                    print("Converted using qlmanage")
            except (subprocess.CalledProcessError, FileNotFoundError):
                pass
        
        # Method 3: Use sips with a workaround (create from scratch)
        if not converted:
            print("Could not convert SVG directly. Creating PNG icon programmatically...")
            create_png_icon_programmatically(script_dir)
            converted = True
        
        if converted and os.path.exists(png_1024):
            # Create all required sizes using sips
            for size in sizes:
                for scale in [1, 2]:
                    actual_size = size * scale if scale == 2 and size < 512 else size
                    if scale == 2 and size >= 512:
                        continue
                    
                    if scale == 1:
                        filename = f"icon_{size}x{size}.png"
                    else:
                        filename = f"icon_{size}x{size}@2x.png"
                    
                    output_path = os.path.join(iconset_dir, filename)
                    subprocess.run(['sips', '-z', str(actual_size), str(actual_size), 
                                  png_1024, '--out', output_path],
                                 capture_output=True)
                    print(f"Created: {filename}")
            
            # Create icns file
            icns_path = os.path.join(script_dir, "AppIcon.icns")
            subprocess.run(['iconutil', '-c', 'icns', iconset_dir, '-o', icns_path],
                         capture_output=True)
            print(f"Created: {icns_path}")
            
    except Exception as e:
        print(f"Error during conversion: {e}")
        print("Please install Pillow (pip install Pillow) or use an online SVG to ICO converter")

def create_png_icon_programmatically(script_dir):
    """Create PNG icon using basic drawing if PIL is available, otherwise create via HTML"""
    
    # Create a simple HTML file that can be screenshotted or use basic shapes
    html_content = '''<!DOCTYPE html>
<html>
<head>
<style>
body { margin: 0; padding: 0; }
.icon {
    width: 1024px;
    height: 1024px;
    background: linear-gradient(135deg, #6366F1 0%, #8B5CF6 50%, #A855F7 100%);
    border-radius: 180px;
    display: flex;
    align-items: center;
    justify-content: center;
    position: relative;
}
.phone {
    font-size: 500px;
    color: white;
}
</style>
</head>
<body>
<div class="icon">
    <span class="phone">📞</span>
</div>
</body>
</html>'''
    
    html_path = os.path.join(script_dir, "icon_preview.html")
    with open(html_path, 'w') as f:
        f.write(html_content)
    print(f"Created HTML preview: {html_path}")
    
    # Try to use screencapture with webkit
    try:
        # Alternative: Create a simple solid color icon with sips
        png_path = os.path.join(script_dir, "AppIcon_1024.png")
        
        # Use Python to create a basic PNG if possible
        try:
            from PIL import Image, ImageDraw
            
            # Create image with gradient
            size = 1024
            img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
            draw = ImageDraw.Draw(img)
            
            # Draw rounded rectangle with gradient effect
            margin = 64
            radius = 180
            
            # Simple gradient approximation
            for y in range(margin, size - margin):
                progress = (y - margin) / (size - 2 * margin)
                r = int(99 + (168 - 99) * progress)
                g = int(102 + (85 - 102) * progress)  
                b = int(241 + (247 - 241) * progress)
                
                for x in range(margin, size - margin):
                    # Check if inside rounded rect
                    in_rect = True
                    # Top-left corner
                    if x < margin + radius and y < margin + radius:
                        if (x - margin - radius)**2 + (y - margin - radius)**2 > radius**2:
                            in_rect = False
                    # Top-right corner
                    elif x > size - margin - radius and y < margin + radius:
                        if (x - (size - margin - radius))**2 + (y - margin - radius)**2 > radius**2:
                            in_rect = False
                    # Bottom-left corner
                    elif x < margin + radius and y > size - margin - radius:
                        if (x - margin - radius)**2 + (y - (size - margin - radius))**2 > radius**2:
                            in_rect = False
                    # Bottom-right corner
                    elif x > size - margin - radius and y > size - margin - radius:
                        if (x - (size - margin - radius))**2 + (y - (size - margin - radius))**2 > radius**2:
                            in_rect = False
                    
                    if in_rect:
                        img.putpixel((x, y), (r, g, b, 255))
            
            # Draw phone icon (simplified)
            # This is a basic representation - for better quality use the SVG
            phone_color = (255, 255, 255, 255)
            
            # Save the image
            img.save(png_path)
            print(f"Created PNG with Pillow: {png_path}")
            
        except ImportError:
            print("Pillow not available. Using alternative method...")
            # Create a placeholder using ImageMagick if available
            try:
                subprocess.run([
                    'convert', '-size', '1024x1024', 
                    'gradient:#6366F1-#A855F7',
                    '-fill', 'white', '-gravity', 'center',
                    '-pointsize', '600', '-annotate', '0', '📞',
                    png_path
                ], check=True, capture_output=True)
                print(f"Created PNG with ImageMagick: {png_path}")
            except (subprocess.CalledProcessError, FileNotFoundError):
                print("ImageMagick not available either.")
                print("\nPlease manually create AppIcon_1024.png or install Pillow:")
                print("  pip3 install Pillow")
                
    except Exception as e:
        print(f"Error: {e}")

if __name__ == "__main__":
    main()
