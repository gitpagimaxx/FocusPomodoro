from pathlib import Path

from PIL import Image, ImageFilter

SOURCE = Path(__file__).with_name("tomato-source.png")
ASSETS = Path(__file__).resolve().parents[1] / "FocusPomodoro" / "Assets"
PLATE = (28, 28, 30, 255)
TARGET_SIZES = (16, 20, 24, 30, 32, 36, 40, 48, 60, 64, 72, 80, 96, 256)


def is_plate(r: int, g: int, b: int) -> bool:
    return r < 50 and g < 50 and b < 50 and abs(r - g) < 12 and abs(g - b) < 12


def cutout(image: Image.Image) -> Image.Image:
    rgba = image.convert("RGBA")
    pixels = rgba.load()
    width, height = rgba.size
    for y in range(height):
        for x in range(width):
            r, g, b, _ = pixels[x, y]
            if is_plate(r, g, b):
                pixels[x, y] = (0, 0, 0, 0)

    bbox = rgba.getbbox()
    if bbox is None:
        return rgba

    left, top, right, bottom = bbox
    pad = 24
    cropped = rgba.crop((
        max(0, left - pad),
        max(0, top - pad),
        min(width, right + pad),
        min(height, bottom + pad),
    ))
    return cropped.filter(ImageFilter.SMOOTH_MORE)


def square_on_transparent(image: Image.Image, size: int) -> Image.Image:
    canvas = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    fitted = image.copy()
    fitted.thumbnail((size, size), Image.Resampling.LANCZOS)
    x = (size - fitted.width) // 2
    y = (size - fitted.height) // 2
    canvas.alpha_composite(fitted, (x, y))
    return canvas


def square_on_plate(image: Image.Image, size: int) -> Image.Image:
    canvas = Image.new("RGBA", (size, size), PLATE)
    inset = max(2, int(size * 0.12))
    fitted = image.copy()
    fitted.thumbnail((size - inset * 2, size - inset * 2), Image.Resampling.LANCZOS)
    x = (size - fitted.width) // 2
    y = (size - fitted.height) // 2
    canvas.alpha_composite(fitted, (x, y))
    return canvas


def save_png(image: Image.Image, name: str, *, keep_alpha: bool) -> None:
    path = ASSETS / name
    to_save = image if keep_alpha else image.convert("RGB")
    to_save.save(path, "PNG", optimize=True)
    print(f"wrote {path.name} {image.size}")


ASSETS.mkdir(parents=True, exist_ok=True)
tomato = cutout(Image.open(SOURCE))

save_png(square_on_plate(tomato, 50), "StoreLogo.png", keep_alpha=False)
save_png(square_on_plate(tomato, 44), "Square44x44Logo.scale-100.png", keep_alpha=False)
save_png(square_on_plate(tomato, 88), "Square44x44Logo.scale-200.png", keep_alpha=False)
save_png(square_on_plate(tomato, 176), "Square44x44Logo.scale-400.png", keep_alpha=False)
save_png(square_on_plate(tomato, 150), "Square150x150Logo.scale-100.png", keep_alpha=False)
save_png(square_on_plate(tomato, 300), "Square150x150Logo.scale-200.png", keep_alpha=False)
save_png(square_on_plate(tomato, 48), "LockScreenLogo.scale-200.png", keep_alpha=False)
save_png(square_on_transparent(tomato, 256), "Tomato.png", keep_alpha=True)

wide = Image.new("RGBA", (620, 300), PLATE)
wide_tomato = square_on_transparent(tomato, 220)
wide.alpha_composite(wide_tomato, ((620 - 220) // 2, (300 - 220) // 2))
save_png(wide, "Wide310x150Logo.scale-200.png", keep_alpha=False)

splash = Image.new("RGBA", (1240, 600), PLATE)
splash_tomato = square_on_transparent(tomato, 320)
splash.alpha_composite(splash_tomato, ((1240 - 320) // 2, (600 - 320) // 2))
save_png(splash, "SplashScreen.scale-200.png", keep_alpha=False)

for size in TARGET_SIZES:
    plated = square_on_plate(tomato, size)
    unplated = square_on_transparent(tomato, size)
    save_png(plated, f"Square44x44Logo.targetsize-{size}.png", keep_alpha=False)
    save_png(unplated, f"Square44x44Logo.targetsize-{size}_altform-unplated.png", keep_alpha=True)
    save_png(unplated, f"Square44x44Logo.targetsize-{size}_altform-lightunplated.png", keep_alpha=True)

ico_path = ASSETS / "AppIcon.ico"
square_on_transparent(tomato, 256).save(
    ico_path,
    format="ICO",
    sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)],
)
print(f"wrote {ico_path.name}")
