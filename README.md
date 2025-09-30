<!--
  NOTE FOR DEVELOPERS:
  This repository has been cleaned up and consolidated into a single, self‑contained
  project.  Historical patch files and temporary README fragments have been removed,
  and all documented fixes have been incorporated directly into the source code.
  See the "Developer Notes" section near the bottom of this file for guidance on
  extending or modifying the application.
-->

# Nuform Estimator & Automation Agent

This solution combines the familiar Nuform estimator (RELINE / RELINE PRO calculator) with
a new automation agent designed to orchestrate data entry across Maximizer, NSD and other
target systems.  On start‑up the application presents a simple menu that lets users
choose one of three workflows:

1. **Automate data entry with file input** – parse a document (e.g. a PDF estimate or `.sof`
   file) and perform data entry in target systems.  This feature is scaffolded and can be
   extended in `AgentPage.xaml.cs`.
2. **Automate data entry using calculator** – run the Nuform calculations behind the scenes and
   push the resulting BOM into external systems.  This is also a stub ready for extension.
3. **RELINE calculator** – launch the traditional estimator UI (IntakePage) to manually create
   jobs, perform calculations and generate `.sof` files, Excel estimates and PDFs.

All existing estimator functionality remains unchanged; the new agent simply wraps the
calculator and exposes additional automation entrypoints.

## Installation & Prerequisites

The estimator still requires Windows with the .NET 8 desktop runtime.  See below for the
original instructions on mapping network drives and configuring `config.json`.  To build or
run the app, open `NuformEstimator.sln` in Visual Studio 2022 with the “.NET desktop
development” workload or run `Build.bat` using the .NET 8 SDK.

## What's New in This Version

- **Automation agent landing page** – a new `AgentPage` presents options on start‑up and
  navigates to the calculator or other workflows.
- **Transition trim fix** – F‑Trim (labelled “Transition” in the UI) now maps internally to
  the J‑Trim category.  The Nuform catalogue does not contain distinct F‑Trim part numbers
  and the previous implementation produced “missing specification” errors.  Mapping to J
  ensures that calculations always resolve a valid part and preserves pack sizing (10 pieces).
- **Code cleanup** – historical patch files (`README-PATCH*.txt`, `.diff`, `.patch` etc.) and
  one‑off build notes have been removed.  All fixes from those documents have been merged
  into the source.  Documentation has been consolidated into this README.
- **Developer notes** – see the section at the bottom of this file for guidance on the
  internal architecture and how to extend the agent.


---

## Original Nuform Estimator Usage

The following section is preserved from the original README.  It describes how to operate
the estimator and what calculations are performed under the hood.



Overview



This Windows app takes your job inputs (rooms, ceilings, openings), does all RELINE/RELINEPRO math, converts trims to full packs, pairs Crown/Base automatically, and outputs:



* A valid .SOF file for Component Order (COP).
* A filled Excel estimate (from your macro template) and a PDF copy.
* Files saved in your existing WIP Estimating and WIP Design folders (no duplicates created).



Prerequisites



Windows with .NET 8 desktop runtime.

Network shares mapped and accessible:

I:\\ACF QUOTES\\WIP Estimating

I:\\WIP Design

Your folders are pre‑created by IT (range folders and child job folders).

(Optional) Excel installed if you want “Fill \& Print Excel”.

config.json placed at
%ProgramData%\\Nuform\\Estimator\\config.json, e.g.:

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
headerLFAdded = headerPanelsAdded \* panelWidthFt.
Converts LF → panels; adds 5% default contingency; rounds up (≤150 to even 2s, >150 to 5s).

Ceilings:

Widthwise: panels = ceil(L / panelWidthFt \* (1+cont)), length = room W.

Lengthwise: perRow ceil(W / panelWidthFt) × rows ceil(L / chosenLen).

Trims:

J‑Trim: packs of 10 pcs at 12’ or 16’ (120/160 LF). Always round packs up.

Corner 90°: packs of 5 pcs at 12’ or 16’ (60/80 LF). Do not add a pack if contingency only tipped it.

Top track via Crown/Base: returns pairs (equal Base + Cap packs).

Calculator works in LF, then converts to full packs using pack LF from the catalog.

Hardware:

Plugs \& Spacers: 1 pack covers 150 panels; then thresholds 200, 250, 300… (n+50 rule).

Expansion tool: 1 up to 250 panels; otherwise 2.

Screws:

Walls: (Σ wallPanels\*panelLenFt + totalWallTrimLF) / 2 → boxes of 500.

Ceilings: (Σ ceilingPanels\*panelLenFt + ceilingTrimLF) / 1.5 → boxes of 500.

Wall screws default Concrete; ceilings Stainless.

Review \& outputs (Results page)

Quantities

See wall/ceiling panel breakdown by length and quantity.

Trims show both LF and packs; Crown/Base shows pairs (Base + Cap, same qty).

Hardware shows packs/boxes totals.

Resolve Server Folders

Click this to find the correct pre‑created job folders on I:\\ (no folders are created).

You should see the WIP Estimating folder path and, if you have a BOM, the WIP Design ...<BOM>\\1-CURRENT\\ path.

Generate .SOF

Ensure a BOM exists in NSD (so there’s a 1-CURRENT folder).

Click Generate .SOF.

The app writes a proper SOF with: panels (pcs), J \& Corner (packs), Crown/Base as two lines with equal packs, and accessories (packs/boxes).

The SOF opens in Component Order.

Fill \& Print Excel (optional)

Click Fill \& Print Excel.

The app opens your macro template, writes items into the appropriate NSD buckets (RELINE/RELINEPRO/Specialty/Other/Shipping), saves a copy in the ESTIMATE folder, then prints to PDF into the same folder using your configured PDF printer.

Open Folder

Opens the resolved job folder in Explorer so you can spot the files immediately.

File naming \& locations (what lands where)

Estimate PDFs/Excel →
I:\\ACF QUOTES\\WIP Estimating<range><Estimate#>\\ESTIMATE  
Filename: <Estimate# \[A] \[R#] \[O#]>.pdf (monikers spaced, e.g., 25885 A R1.pdf)

Drawings → ...\\DRAWINGS\\ named "<Estimate# monikers> - Drawing.pdf".

Invoicing PDFs → ...\\INVOICING\\ named "<Estimate# monikers> - Email 1.pdf" etc.

.SOF →
I:\\WIP Design<range><BOM>\\1-CURRENT<BOM>.sof
(If BOM isn’t created yet, create it in NSD first so the folder exists. The app won’t create it for you.)

Changing colors / lengths

Color: Default is BRIGHT WHITE. If your UI exposes color, select it; otherwise add the color to the parts catalog and set it as default or per‑job.

Trim lengths: Default to 16’ packs for J/Corner/Crown/Base (configurable). You can switch to 12’—the pack LF changes accordingly (J 120 LF vs 160 LF; Corner 60 LF vs 80 LF).

Updating the parts catalog

The app uses a CatalogService to map item requests → real part codes.

To add colors or new parts, edit Nuform.Core/Data/parts.csv (or update the PDF‑backed store as IT prefers):

Include: PartNumber,Description,Units,PackPieces,LengthFt,Color,Category.

## Developer Notes

### Project structure

The solution consists of two major projects:

- **Nuform.Core** – domain models and calculation logic for RELINE / RELINEPRO jobs.  This
  layer has no UI dependencies and can be tested in isolation.  Notable types include
  `CeilingCalc`, `TrimPolicy`, `CatalogService` and `BomService`.
- **Nuform.App** – a WPF application that hosts the UI.  `MainWindow.xaml` contains a
  `Frame` which navigates between pages such as `AgentPage` and `IntakePage`.  View models
  reside in `Nuform.App/ViewModels`.

For automation beyond the estimator, see the companion `automation-foundation` project.
It provides a FastAPI service, a Redis/RQ worker and Playwright scripts to drive
Maximizer and NSD.  You can integrate those workflows by wiring the buttons on
`AgentPage` to call into the automation APIs or run the worker locally.

### Extending the automation agent

`AgentPage.xaml.cs` contains stubs for the two new automation workflows.  To implement
file‑based automation:

1. Create a new WPF page (e.g. `FileInputAutomationPage`) that lets the user select a PDF,
   Word document or `.sof` file.
2. Parse the document using Python (see `automation/estimate_parser.py` for an example) or
   a .NET PDF/text library.
3. Submit the parsed data to your downstream systems via HTTP or call into the
   `automation-foundation` API.

Similarly, calculator‑driven automation can read data from the existing view models
(`CalculationsViewModel`) and dispatch them to external systems.

### Packaging and deployment

To produce a distributable build, run `Build.bat` at the root of the repository or
compile `Nuform.App` in Release mode via Visual Studio.  The published app will include
the WPF executable, dependency DLLs and the `Data/parts.csv` catalog.

### Removing patch files

All historical patch files have been deleted from this repository.  If you need to refer
to previous fixes, check the commit history in your source control system.  The code now
reflects the latest logic for ceiling orientation, H‑Trim pack sizes and trim length
decisions.

Key categories: Panel, J, Corner90, CrownBaseBase, CrownBaseCap, Cove, Drip, F, H, Accessory.Plugs, Accessory.Spacers, Accessory.ExpansionTool, Accessory.ConcreteScrews, Accessory.StainlessScrews.

Troubleshooting

“.SOF not created: folder does not exist”
→ Create/select a BOM in NSD so ...<BOM>\\1-CURRENT\\ exists, then try again.

Component Order won’t open the SOF

Ensure the file has CRLF newlines and ANSI (Windows‑1252) encoding.

Panels must have width in column 4 (18 or 12); trims/accessories must have 1 there.

No folders found

Confirm I:\\ paths in config.json.

Make sure your estimate/BOM folders were pre‑created by IT (the app does not create them).

Excel won’t fill/print

Verify excelTemplatePath and that Excel is installed.

Check printer name in config (e.g., “Microsoft Print to PDF”).

Quick checklist (daily use)

Enter rooms, ceilings, and openings on Intake → Next.

Review Results (panels, trims, hardware).

Resolve Server Folders.

Generate .SOF (BOM must exist).

Fill \& Print Excel (optional).

Open Folder to verify files.


## Playwright quickstart

```bash
# install deps
npm i

# install browsers (first time only)
npm run install:pw

# run unit tests (domain calc)
npm test

# run e2e smoke in headless and headed modes
npm run e2e
npm run e2e:headed
```

## Auth & Download automation (Playwright)

### 0) Set environment variables
Create a `.env` file or set variables in your shell with your real credentials/URLs:

```
MAX_BASE_URL=http://crm.nuformdirect.com/MaximizerWebAccess/Default.aspx
MAX_USER=your_max_user
MAX_PASS=your_max_pass

NSD_BASE_URL=https://your-nsd.example.com/login
NSD_USER=your_nsd_user
NSD_PASS=your_nsd_pass

# Optional:
RUNS_DIR=runs
```

> **Windows Command Prompt (cmd.exe)** examples:
```
set MAX_BASE_URL=http://crm.nuformdirect.com/MaximizerWebAccess/Default.aspx
set MAX_USER=you@example.com
set MAX_PASS=secret
set NSD_BASE_URL=https://your-nsd.example.com/login
set NSD_USER=you@example.com
set NSD_PASS=secret
```

> **PowerShell** examples:
```
$env:MAX_BASE_URL="http://crm.nuformdirect.com/MaximizerWebAccess/Default.aspx"
$env:MAX_USER="you@example.com"
$env:MAX_PASS="secret"
$env:NSD_BASE_URL="https://your-nsd.example.com/login"
$env:NSD_USER="you@example.com"
$env:NSD_PASS="secret"
```

### 1) Generate storage state (login once)
```
npm run e2e:auth:max   # saves storageState/maximizer.json
npm run e2e:auth:nsd   # saves storageState/nsd.json
```

> If a command is *skipped*, check that the related env vars are set.  
> You may need to update the selectors inside `tests/setup/auth.setup.spec.ts` to match the real login form.

### 2) Download the System Estimate PDF
```
npm run e2e:download
```
This uses the saved NSD storage state and will place the file under `runs/<timestamp>/`.

### Notes on Windows shells
- **cmd.exe** uses `set VAR=value` to set env vars for the current session.
- **PowerShell** uses `$env:VAR="value"`.
- To set an env var *inline* for one command in **PowerShell**:
  ```powershell
  $env:NSD_BASE_URL="https://..." ; npm run e2e:download
  ```
- To make variables persistent, add them to a `.env` and use a loader like `dotenv-cli` (optional) or set them in System Properties → Environment Variables.
