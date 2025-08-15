# estimator-RPA
Automating Estimation

Nuform Estimator — How to Use
Overview

This Windows app takes your job inputs (rooms, ceilings, openings), does all RELINE/RELINEPRO math, converts trims to full packs, pairs Crown/Base automatically, and outputs:

A valid .SOF file for Component Order (COP).

A filled Excel estimate (from your macro template) and a PDF copy.

Files saved in your existing WIP Estimating and WIP Design folders (no duplicates created).

Prerequisites

Windows with .NET 8 desktop runtime.

Network shares mapped and accessible:

I:\ACF QUOTES\WIP Estimating

I:\WIP Design

Your folders are pre‑created by IT (range folders and child job folders).

(Optional) Excel installed if you want “Fill & Print Excel”.

config.json placed at
%ProgramData%\Nuform\Estimator\config.json, e.g.:

{
  "wipEstimatingRoot": "I:/ACF QUOTES/WIP Estimating",
  "wipDesignRoot": "I:/WIP Design",
  "pdfPrinter": "Microsoft Print to PDF",
  "excelTemplatePath": "I:/Shared/Templates/Estimating Template-v.2025.5.23 (64bit).xlsm"
}


Parts catalog available to the app (CSV or PDF‑backed). Default color is BRIGHT WHITE; add other colors by extending the catalog.

One‑time setup

Launch the app (Nuform Estimator).

Open Settings (or use the config file) to confirm:

Paths to WIP roots are correct.

Excel template path is set (if using Excel output).

Default PDF printer is correct.

Start a job (Intake page)

Enter Estimate # (and contingency % if different than 5%).

In Rooms, add each space:

L, W, H (ft).

WallPanelLen (choose a standard even length, 10–20 ft; e.g., 14’).

PanelWidth = 18 for RELINE PRO (default) or 12 for RELINE.

Ceiling: check if included.

CeilingLen (only used for lengthwise).

Orientation: Widthwise (preferred) or Lengthwise.

In Openings, add each door/window type:

Width, Height, Count.

HeaderH, SillH (doors usually SillH=0; windows split header+sill).

(If your UI has wrap options: choose J-Trim vs Corner, and Recessed if needed.)

Click Next.

What the calculator does (so you know what to expect)

Walls: uses total perimeter LF minus opening widths + header add‑back using
piecesPerFull = panelLenFt / (headerH + sillH);
headerPanelsAdded = openingWidth / piecesPerFull;
headerLFAdded = headerPanelsAdded * panelWidthFt.
Converts LF → panels; adds 5% default contingency; rounds up (≤150 to even 2s, >150 to 5s).

Ceilings:

Widthwise: panels = ceil(L / panelWidthFt * (1+cont)), length = room W.

Lengthwise: perRow ceil(W / panelWidthFt) × rows ceil(L / chosenLen).

Trims:

J‑Trim: packs of 10 pcs at 12’ or 16’ (120/160 LF). Always round packs up.

Corner 90°: packs of 5 pcs at 12’ or 16’ (60/80 LF). Do not add a pack if contingency only tipped it.

Top track via Crown/Base: returns pairs (equal Base + Cap packs).

Calculator works in LF, then converts to full packs using pack LF from the catalog.

Hardware:

Plugs & Spacers: 1 pack covers 150 panels; then thresholds 200, 250, 300… (n+50 rule).

Expansion tool: 1 up to 250 panels; otherwise 2.

Screws:

Walls: (Σ wallPanels*panelLenFt + totalWallTrimLF) / 2 → boxes of 500.

Ceilings: (Σ ceilingPanels*panelLenFt + ceilingTrimLF) / 1.5 → boxes of 500.

Wall screws default Concrete; ceilings Stainless.

Review & outputs (Results page)

Quantities

See wall/ceiling panel breakdown by length and quantity.

Trims show both LF and packs; Crown/Base shows pairs (Base + Cap, same qty).

Hardware shows packs/boxes totals.

Resolve Server Folders

Click this to find the correct pre‑created job folders on I:\ (no folders are created).

You should see the WIP Estimating folder path and, if you have a BOM, the WIP Design ...\<BOM>\1-CURRENT\ path.

Generate .SOF

Ensure a BOM exists in NSD (so there’s a 1-CURRENT folder).

Click Generate .SOF.

The app writes a proper SOF with: panels (pcs), J & Corner (packs), Crown/Base as two lines with equal packs, and accessories (packs/boxes).

The SOF opens in Component Order.

Fill & Print Excel (optional)

Click Fill & Print Excel.

The app opens your macro template, writes items into the appropriate NSD buckets (RELINE/RELINEPRO/Specialty/Other/Shipping), saves a copy in the ESTIMATE folder, then prints to PDF into the same folder using your configured PDF printer.

Open Folder

Opens the resolved job folder in Explorer so you can spot the files immediately.

File naming & locations (what lands where)

Estimate PDFs/Excel →
I:\ACF QUOTES\WIP Estimating\<range>\<Estimate#>\ESTIMATE\
Filename: <Estimate# [A] [R#] [O#]>.pdf (monikers spaced, e.g., 25885 A R1.pdf)

Drawings → ...\DRAWINGS\ named "<Estimate# monikers> - Drawing.pdf".

Invoicing PDFs → ...\INVOICING\ named "<Estimate# monikers> - Email 1.pdf" etc.

.SOF →
I:\WIP Design\<range>\<BOM>\1-CURRENT\<BOM>.sof
(If BOM isn’t created yet, create it in NSD first so the folder exists. The app won’t create it for you.)

Changing colors / lengths

Color: Default is BRIGHT WHITE. If your UI exposes color, select it; otherwise add the color to the parts catalog and set it as default or per‑job.

Trim lengths: Default to 16’ packs for J/Corner/Crown/Base (configurable). You can switch to 12’—the pack LF changes accordingly (J 120 LF vs 160 LF; Corner 60 LF vs 80 LF).

Updating the parts catalog

The app uses a CatalogService to map item requests → real part codes.

To add colors or new parts, edit Nuform.Core/Data/parts.csv (or update the PDF‑backed store as IT prefers):

Include: PartNumber,Description,Units,PackPieces,LengthFt,Color,Category.

Key categories: Panel, J, Corner90, CrownBaseBase, CrownBaseCap, Cove, Drip, F, H, Accessory.Plugs, Accessory.Spacers, Accessory.ExpansionTool, Accessory.ConcreteScrews, Accessory.StainlessScrews.

Troubleshooting

“.SOF not created: folder does not exist”
→ Create/select a BOM in NSD so ...\<BOM>\1-CURRENT\ exists, then try again.

Component Order won’t open the SOF

Ensure the file has CRLF newlines and ANSI (Windows‑1252) encoding.

Panels must have width in column 4 (18 or 12); trims/accessories must have 1 there.

No folders found

Confirm I:\ paths in config.json.

Make sure your estimate/BOM folders were pre‑created by IT (the app does not create them).

Excel won’t fill/print

Verify excelTemplatePath and that Excel is installed.

Check printer name in config (e.g., “Microsoft Print to PDF”).

Quick checklist (daily use)

Enter rooms, ceilings, and openings on Intake → Next.

Review Results (panels, trims, hardware).

Resolve Server Folders.

Generate .SOF (BOM must exist).

Fill & Print Excel (optional).

Open Folder to verify files.
