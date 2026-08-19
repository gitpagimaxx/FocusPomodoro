from pathlib import Path

from PIL import Image

SOURCE = Path(__file__).with_name("tomato-source.png")
ASSETS = Path(__file__).resolve().parents[1] / "FocusPomodoro" / "Assets"
BG = (28, 28, 30)

source = Image.open(SOURCE).convert("RGBA")


def fit_square(size: int) -> Image.Image:
    return source.resize((size, size), Image.Resampling.LANCZOS)


def on_canvas(width: int, height: int, tomato_size: int) -> Image.Image:
    canvas = Image.new("RGBA", (width, height), BG + (255,))
    tomato = fit_square(tomato_size)
    x = (width - tomato_size) // 2
    y = (height - tomato_size) // 2
    canvas.alpha_composite(tomato, (x, y))
    return canvas


def save_png(image: Image.Image, name: str) -> None:
    path = ASSETS / name
    image.convert("RGB").save(path, "PNG", optimize=True)
    print(f"wrote {path.name} {image.size}")


ASSETS.mkdir(parents=True, exist_ok=True)

save_png(fit_square(50), "StoreLogo.png")
save_png(fit_square(88), "Square44x44Logo.scale-200.png")
save_png(fit_square(24), "Square44x44Logo.targetsize-24_altform-unplated.png")
save_png(fit_square(300), "Square150x150Logo.scale-200.png")
save_png(fit_square(48), "LockScreenLogo.scale-200.png")
save_png(fit_square(256), "Tomato.png")
save_png(on_canvas(620, 300, 240), "Wide310x150Logo.scale-200.png")
save_png(on_canvas(1240, 600, 360), "SplashScreen.scale-200.png")

ico_path = ASSETS / "AppIcon.ico"
ico_sizes = [(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)]
fit_square(256).save(ico_path, format="ICO", sizes=ico_sizes)
print(f"wrote {ico_path.name}")
