# SkywardPaletteSharp

*Skyward Sword HD*'s stage palette tables, which the game loads from each stage's palette archive: `.spf`/`.pal`
(lighting palettes), `.sff` (fog), `.eaf` (effect palettes) and `.mcf` (colour tables).

```csharp
PaletteFogFile fog = PaletteFogFile.FromBinary(arc["palette/B100.sff"]!.Data);
foreach (PaletteFog f in fog.Entries) Console.WriteLine(f);           // colour, near, far
fog.Entries[0].Far = 20000;

PaletteFile spf = PaletteFile.FromBinary(arc["palette/B100.spf"]!.Data);
PaletteRecord palette = spf.Palettes[0];                                // 520 bytes = 130 words
palette.SetColor(0, 0x505073FF);
float f6 = palette.Float(6);
byte[] packed = spf.Write();
```

Files are kept as bytes and edited in place. An `.spf` is a u32 and 20 palette records, a `.pal` a u32 and one (the
560-byte ones carry 36 bytes more, kept as `Tail`). An `.sff` is a u32 and 60 fog entries. An `.eaf` is 32 records of
160 bytes (128 in one file), and an `.mcf` 16 RGBA colours. What each word of a palette or effect record means is not
known yet, so records are read and written by word index as a colour, float or u32.

All 402 distinct palette files in the dump (184 `.spf`, 36 `.pal`, 124 `.sff`, 56 `.eaf`, 2 `.mcf`) parse, take a
value written back through each view, and rebuild to the same bytes.

## Build

```bash
dotnet build SkywardPaletteSharp.slnx -c Release
```

## Licence

AGPL-3.0-or-later. See [license.md](license.md).
