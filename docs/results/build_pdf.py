"""Render the Results markdown to a styled PDF via Chrome headless.

Usage:  python docs/results/build_pdf.py
"""
import subprocess
import sys
from pathlib import Path

import markdown

HERE = Path(__file__).parent
MD = HERE / "AdvSoftTech_Assignment-Results.md"
HTML = HERE / "AdvSoftTech_Assignment-Results.html"
PDF = HERE / "AdvSoftTech_Assignment-Results.pdf"

CHROME = r"C:\Program Files\Google\Chrome\Application\chrome.exe"

CSS = """
@page { size: A4; margin: 18mm 16mm; }
body { font-family: 'Segoe UI', Calibri, Arial, sans-serif; font-size: 10.5pt;
       line-height: 1.45; color: #1a1a1a; }
h1 { font-size: 22pt; color: #2E75B6; border-bottom: 2px solid #2E75B6;
     padding-bottom: 6px; margin: 0 0 4px; }
h2 { font-size: 14pt; color: #2E75B6; margin-top: 22px;
     border-bottom: 1px solid #d0d7de; padding-bottom: 3px; }
h3 { font-size: 11.5pt; color: #333; margin-top: 16px; }
h4 { font-size: 10.5pt; color: #444; margin-top: 12px; }
table { border-collapse: collapse; width: 100%; margin: 10px 0; font-size: 9pt; }
th { background: #2E75B6; color: #fff; text-align: left; padding: 5px 7px; }
td { border: 1px solid #ccc; padding: 4px 7px; vertical-align: top; }
tr:nth-child(even) td { background: #f4f7fb; }
blockquote { border-left: 4px solid #2E75B6; background: #eef4fb; margin: 10px 0;
             padding: 6px 12px; color: #333; }
code { background: #eef1f4; padding: 1px 4px; border-radius: 3px;
       font-family: Consolas, monospace; font-size: 9pt; }
pre { background: #f4f7fb; border: 1px solid #d0d7de; border-radius: 4px;
      padding: 8px 10px; overflow-x: auto; }
pre code { background: none; padding: 0; }
hr { border: none; border-top: 1px solid #d0d7de; margin: 18px 0; }
a { color: #2E75B6; text-decoration: none; }
"""

def main() -> int:
    md_text = MD.read_text(encoding="utf-8")
    body = markdown.markdown(
        md_text,
        extensions=["tables", "fenced_code", "sane_lists", "pymdownx.tilde"],
    )
    html = (
        f"<!doctype html><html><head><meta charset='utf-8'>"
        f"<style>{CSS}</style></head><body>{body}</body></html>"
    )
    HTML.write_text(html, encoding="utf-8")

    result = subprocess.run(
        [
            CHROME,
            "--headless",
            "--disable-gpu",
            "--no-pdf-header-footer",
            f"--print-to-pdf={PDF}",
            HTML.as_uri(),
        ],
        capture_output=True,
        text=True,
    )
    if not PDF.exists():
        print(result.stderr, file=sys.stderr)
        return 1
    print(f"Wrote {PDF} ({PDF.stat().st_size // 1024} KB)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
