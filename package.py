"""Build the local Thunderstore import archive from the Release build."""
import json
import struct
from pathlib import Path
from tempfile import TemporaryDirectory
from xml.etree import ElementTree
from zipfile import ZIP_DEFLATED, ZipFile


root = Path(__file__).resolve().parent
listing = root / "thunderstore"
project = ElementTree.parse(root / "ScoutsMap.csproj")
manifest = json.loads((listing / "manifest.json").read_text(encoding="utf-8-sig"))
name, version = manifest["name"], manifest["version_number"]
if name != "ScoutsMap" or version != project.findtext(".//Version"):
    raise SystemExit("Manifest and project name or version differ.")

framework = project.findtext(".//TargetFramework")
files = {
    "manifest.json": listing / "manifest.json",
    "README.md": listing / "README.md",
    "icon.png": listing / "icon.png",
    "CHANGELOG.md": root / "CHANGELOG.md",
    "LICENSE": root / "LICENSE",
    f"plugins/{name}.dll": root / "bin" / "Release" / framework / f"{name}.dll",
}
icon = files["icon.png"].read_bytes()
if icon[:8] != b"\x89PNG\r\n\x1a\n" or struct.unpack(">II", icon[16:24]) != (256, 256):
    raise SystemExit("Expected a 256 by 256 PNG icon.")

output = root / "dist" / f"{name}-{version}.zip"
output.parent.mkdir(exist_ok=True)
with TemporaryDirectory(dir=output.parent) as scratch:
    candidate = Path(scratch) / output.name
    with ZipFile(candidate, "w", compression=ZIP_DEFLATED) as archive:
        for member, path in files.items():
            archive.write(path, member)
    with ZipFile(candidate) as archive:
        if archive.testzip() or sorted(archive.namelist()) != sorted(files):
            raise SystemExit("Invalid ZIP structure or checksum.")
        for member, path in files.items():
            if archive.read(member) != path.read_bytes():
                raise SystemExit(f"Verification failed for {member}.")
    candidate.replace(output)
print(output)
