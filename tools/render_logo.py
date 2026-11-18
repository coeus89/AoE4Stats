"""Draw the original AoE4 Stats mark. No game artwork is used."""

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "src" / "AoE4Stats" / "Assets"
BRAND = ROOT / "brand"
INK = (22, 52, 63, 255)
BRASS = (224, 193, 122, 255)
CREAM = (247, 241, 225, 255)
FONT = "/usr/share/fonts/truetype/macos/Inter-SemiBold.ttf"


def draw_mark(size: int, plated: bool, wide: bool = False, splash: bool = False) -> Image.Image:
    scale = 4
    if splash:
        width, height = size, int(round(size * 300 / 620))
    elif wide:
        width, height = size, int(round(size * 150 / 310))
    else:
        width = height = size
    canvas_w, canvas_h = width * scale, height * scale
    image = Image.new("RGBA", (canvas_w, canvas_h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    if plated:
        radius = max(8, int(min(canvas_w, canvas_h) * 0.18))
        draw.rounded_rectangle((0, 0, canvas_w - 1, canvas_h - 1), radius=radius, fill=INK)

    mark_color = BRASS if plated else BRASS
    pad = int(min(canvas_w, canvas_h) * (0.22 if not splash else 0.28))
    if splash or wide:
        box = int(min(canvas_h * 0.62, canvas_w * 0.28))
        left = int(canvas_w * 0.08)
        top = (canvas_h - box) // 2
    else:
        box = canvas_w - pad * 2
        left = pad
        top = pad

    # Rising bars: a rating chart, not a faction emblem.
    gap = max(2, box // 18)
    bar_w = max(4, (box - gap * 2) // 3)
    heights = (0.42, 0.66, 0.92)
    base = top + box
    for index, ratio in enumerate(heights):
        x0 = left + index * (bar_w + gap)
        y0 = base - int(box * ratio)
        draw.rounded_rectangle((x0, y0, x0 + bar_w, base), radius=bar_w // 3, fill=mark_color)

    diamond = max(6, bar_w)
    cx = left + box // 2
    cy = top + int(box * 0.08)
    draw.polygon(
        [(cx, cy - diamond), (cx + diamond, cy), (cx, cy + diamond), (cx - diamond, cy)],
        fill=CREAM if plated else BRASS,
    )

    if (splash or wide) and width >= 300:
        font = ImageFont.truetype(FONT, size=int(canvas_h * (0.22 if splash else 0.28)))
        text = "AoE4 Stats"
        draw.text((left + box + int(canvas_w * 0.06), canvas_h * 0.34), text, font=font, fill=CREAM)
        small = ImageFont.truetype(FONT, size=int(canvas_h * 0.09))
        draw.text((left + box + int(canvas_w * 0.06), canvas_h * 0.60), "Player lookup", font=small, fill=BRASS)

    return image.resize((width, height), Image.Resampling.LANCZOS)


def save_png(image: Image.Image, name: str) -> None:
    image.save(ASSETS / name, format="PNG")


def main() -> None:
    ASSETS.mkdir(parents=True, exist_ok=True)
    BRAND.mkdir(parents=True, exist_ok=True)
    plated = {
        "StoreLogo.png": draw_mark(50, True),
        "Square44x44Logo.png": draw_mark(44, True),
        "Square44x44Logo.scale-200.png": draw_mark(88, True),
        "Square150x150Logo.png": draw_mark(150, True),
        "Square150x150Logo.scale-200.png": draw_mark(300, True),
        "SmallTile.png": draw_mark(71, True),
        "SmallTile.scale-200.png": draw_mark(142, True),
        "LargeTile.png": draw_mark(310, True),
        "LargeTile.scale-200.png": draw_mark(620, True),
        "TitleIcon.png": draw_mark(44, True),
    }
    for name, image in plated.items():
        save_png(image, name)

    wide = draw_mark(310, True, wide=True)
    save_png(wide, "Wide310x150Logo.png")
    save_png(draw_mark(620, True, wide=True), "Wide310x150Logo.scale-200.png")
    splash = draw_mark(620, True, splash=True)
    save_png(splash, "SplashScreen.png")
    save_png(draw_mark(1240, True, splash=True), "SplashScreen.scale-200.png")

    for target in (16, 24, 32, 48, 256):
        save_png(draw_mark(target, False), f"Square44x44Logo.targetsize-{target}_altform-unplated.png")

    icon_sizes = [16, 24, 32, 48, 64, 256]
    icons = [draw_mark(size, True) for size in icon_sizes]
    icons[-1].save(
        ASSETS / "AppIcon.ico",
        format="ICO",
        sizes=[(size, size) for size in icon_sizes],
        append_images=icons[:-1],
    )
    (BRAND / "mark.svg").write_text(
        """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 64 64" role="img" aria-label="AoE4 Stats mark">
  <rect width="64" height="64" rx="12" fill="#16343F"/>
  <polygon points="32,8 40,16 32,24 24,16" fill="#F7F1E1"/>
  <rect x="14" y="36" width="10" height="16" rx="2" fill="#E0C17A"/>
  <rect x="27" y="26" width="10" height="26" rx="2" fill="#E0C17A"/>
  <rect x="40" y="16" width="10" height="36" rx="2" fill="#E0C17A"/>
</svg>
""",
        encoding="utf-8",
    )


if __name__ == "__main__":
    main()
