from __future__ import annotations

import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageOps


source_directory = Path(sys.argv[1])
output_directory = Path(sys.argv[2])
output_directory.mkdir(parents=True, exist_ok=True)

pages = sorted(source_directory.glob("page-*.png"))
if not pages:
    raise SystemExit("No rendered PDF pages were found.")

max_width = max(Image.open(path).width for path in pages)
max_height = max(Image.open(path).height for path in pages)
cell_width = max_width + 12
cell_height = max_height + 12

for batch_start in range(0, len(pages), 4):
    batch = pages[batch_start : batch_start + 4]
    sheet = Image.new("RGB", (cell_width * 2, cell_height * 2), "white")
    draw = ImageDraw.Draw(sheet)
    for index, page_path in enumerate(batch):
        with Image.open(page_path) as source:
            page = source.convert("RGB")
        x = (index % 2) * cell_width
        y = (index // 2) * cell_height
        sheet.paste(ImageOps.expand(page, border=4, fill="gray"), (x, y))
        draw.text((x + 14, y + 14), page_path.stem, fill="red")
    sheet.save(output_directory / f"sheet-{batch_start // 4 + 1:02}.png")

print(f"pages={len(pages)} sheets={len(list(output_directory.glob('sheet-*.png')))} max={max_width}x{max_height}")
