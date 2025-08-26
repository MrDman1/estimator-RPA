// pvcEstimator.ts
export type PanelWidth = 12 | 18;
export type TrimLen = 12 | 16;

export interface Room {
  id: string;
  name: string;
  L: number; // ft
  W: number; // ft
  H: number; // ft
  wallPanelLen: number; // chosen std length 10..20 even (or custom)
  panelWidth: PanelWidth; // 12 or 18 (inches)
  ceiling: {
    include: boolean;
    orientation: "widthwise" | "lengthwise";
    lengthwisePanelLen?: number; // if orientation=lengthwise
    useCustomCut?: boolean;
  };
}

export interface Opening {
  id: string;
  where: string; // room id
  type: "manDoor" | "garageDoor" | "window" | "other";
  width: number;      // ft
  height: number;     // ft
  count: number;
  headerHeight: number; // ft
  sillHeight: number;   // ft (doors typically 0)
  wrap: "jTrim" | "cornerTrim";
  recessed?: boolean;          // only relevant if wrap=jTrim
  includeSillInWrap?: boolean; // default false for doors, true for windows
}

export interface TrimChoices {
  topTrack: "crownBase" | "doubleJ" | "cove" | "fTrim" | "corner" | "none";
  jTrimPackLen: TrimLen;       // 12 -> 120 LF/pack, 16 -> 160 LF/pack
  cornerTrimPackLen: TrimLen;  // 12 -> 60 LF/pack, 16 -> 80 LF/pack
}

export interface Pricing {
  panelSqFtPrice12: number;
  panelSqFtPrice18: number;
  trimsLF: {
    h: number; f: number; j: number; corner90: number; cornerAngle: number; cove: number; drip: number; crownBase: number;
  };
  accessories: {
    spacers: number;        // per 20
    plugs: number;          // per 100
    concreteScrew: number;  // per 500
    stainlessScrew: number; // per 500
    expansionTool: number;  // per 5
  };
}

export interface Result {
  wallPanels: { qty: number; length: number; width: PanelWidth };
  ceilingPanels?: { qty: number; length: number; width: PanelWidth };
  trims: {
    jTrim: { lf: number; packs: number; packLen: TrimLen };
    cornerTrim: { lf: number; packs: number; packLen: TrimLen };
    topTrack?: { lf: number; packs: number; kind: TrimChoices["topTrack"]; packLen: TrimLen };
  };
  hardware: {
    plugsPacks: number;
    spacersPacks: number;
    expansionTools: number;
    wallScrewBoxes: number;
    ceilingScrewBoxes: number;
  };
  nsdBuckets?: { RELINE: number; RELINEPRO: number; Specialty: number; Other: number; Shipping: number };
}

const PANEL_WIDTH_FT = (w: PanelWidth) => w / 12;
const PACK_LF = {
  J: (len: TrimLen) => (len === 12 ? 120 : 160),
  Corner: (len: TrimLen) => (len === 12 ? 60 : 80),
};
const SCREWS_PER_BOX = 500;

export function packsForPanels(totalPanels: number): number {
  if (totalPanels <= 0) return 0;                      // 0 panels -> 0 packs
  // Thresholds: 150, 200, 250, 300, ...
  return Math.max(1, Math.ceil((totalPanels - 100) / 50));
}

const roundPanels = (qty: number) =>
  qty <= 150 ? Math.ceil(qty / 2) * 2 : Math.ceil(qty / 5) * 5;

function headerAddBackPanels(panelLen: number, openW: number, headerH: number, sillH: number) {
  const denom = headerH + sillH;
  if (denom <= 0) return 0;
  const piecesPerFull = panelLen / denom; // corrected rule
  return openW / piecesPerFull;
}

export function calcEstimate(
  rooms: Room[],
  openings: Opening[],
  trims: TrimChoices,
  opts?: {
    contingencyPct?: number;   // default 5
    topTrackPackLen?: TrimLen; // default = jTrimPackLen
    useMetalWallScrews?: boolean; // label only
  },
  pricing?: Pricing // optional; if provided, populate nsdBuckets
): Result {
  const contingency = (opts?.contingencyPct ?? 5) / 100;
  const topPackLen = opts?.topTrackPackLen ?? trims.jTrimPackLen;

  if (!rooms.length) {
    return {
      wallPanels: { qty: 0, length: 0, width: 18 },
      trims: { jTrim: { lf: 0, packs: 0, packLen: trims.jTrimPackLen }, cornerTrim: { lf: 0, packs: 0, packLen: trims.cornerTrimPackLen } },
      hardware: { plugsPacks: 0, spacersPacks: 0, expansionTools: 0, wallScrewBoxes: 0, ceilingScrewBoxes: 0 },
    };
  }

  // Assume a common panel width & wall panel length for now (as per current workflow)
  const ref = rooms[0];
  const panelWidthFt = PANEL_WIDTH_FT(ref.panelWidth);
  const wallPanelLen = ref.wallPanelLen;

  // 1) Total wall perimeter LF
  const totalPerimeterLF = rooms.reduce((s, r) => s + 2 * (r.L + r.W), 0);

  // 2) Openings LF subtraction + trim accumulation
  let openingLFSubtract = 0;
  let jTrimLF = 0;
  let cornerTrimLF = 0;

  for (const op of openings) {
    // perimeter for trim wrap (no sill by default on doors)
    const basePerim = (op.width + 2 * op.height);
    const perim = op.includeSillInWrap ? basePerim + op.width : basePerim;
    if (op.wrap === "cornerTrim") {
      cornerTrimLF += perim * op.count;
    } else {
      const factor = op.recessed ? 3 : 1;
      jTrimLF += factor * perim * op.count;
    }

    // panel LF delta
    const addBackPanels = headerAddBackPanels(wallPanelLen, op.width, op.headerHeight, op.sillHeight) * op.count;
    const addBackLF = addBackPanels * panelWidthFt;
    openingLFSubtract += (op.width * op.count) - addBackLF;
  }

  // 3) Wall panels with contingency + rounding
  const netLF = totalPerimeterLF - openingLFSubtract;
  const basePanels = netLF / panelWidthFt;
  const wallPanelsQty = roundPanels(basePanels * (1 + contingency));

  // 4) Ceiling panels
  let ceilingPanelsQty = 0;
  let ceilingPanelLen = 0;
  for (const r of rooms) {
    if (!r.ceiling?.include) continue;
    if (r.ceiling.orientation === "widthwise") {
      const across = Math.ceil(r.W / panelWidthFt);
      const runs = Math.ceil(r.L / (r.ceiling.lengthwisePanelLen ?? r.wallPanelLen));
      const qty = Math.ceil(across * runs * (1 + contingency));
      ceilingPanelsQty += qty;
      ceilingPanelLen = Math.max(ceilingPanelLen, r.ceiling.lengthwisePanelLen ?? r.wallPanelLen);
    } else {
      const across = Math.ceil(r.L / panelWidthFt);
      const runs = Math.ceil(r.W / (r.ceiling.lengthwisePanelLen ?? r.wallPanelLen));
      const qty = Math.ceil(across * runs * (1 + contingency));
      ceilingPanelsQty += qty;
      ceilingPanelLen = Math.max(ceilingPanelLen, r.ceiling.lengthwisePanelLen ?? r.wallPanelLen);
    }
  }

  // 5) Top track LF (rooms with ceilings)
  let topTrackLF = 0;
  if (trims.topTrack !== "none") {
    const perimeterWithCeiling = rooms.filter(r => r.ceiling?.include).reduce((s, r) => s + 2 * (r.L + r.W), 0);
    topTrackLF = Math.ceil(perimeterWithCeiling * (1 + contingency));
  }

  // Apply contingency to trims & compute packs
  const preContCorner = cornerTrimLF; // for no-extra-pack-if-contingency-only rule
  jTrimLF = Math.ceil(jTrimLF * (1 + contingency));
  cornerTrimLF = Math.ceil(cornerTrimLF * (1 + contingency));

  const jPacks = Math.ceil(jTrimLF / PACK_LF.J(trims.jTrimPackLen));
  let cornerPacks = Math.floor(cornerTrimLF / PACK_LF.Corner(trims.cornerTrimPackLen));
  if (cornerPacks * PACK_LF.Corner(trims.cornerTrimPackLen) < cornerTrimLF) {
    // only add a pack if pre-contingency also required it
    const basePacks = Math.ceil(preContCorner / PACK_LF.Corner(trims.cornerTrimPackLen));
    cornerPacks = Math.max(cornerPacks, basePacks);
  }
  const topPacks = trims.topTrack === "none" ? undefined
    : Math.ceil(topTrackLF / PACK_LF.J(topPackLen));

  // 6) Hardware
  const totalPanels = wallPanelsQty + ceilingPanelsQty;
  const plugsPacks = packsForPanels(totalPanels);
  const spacersPacks = packsForPanels(totalPanels);
  const expansionTools = totalPanels > 250 ? 2 : 1;

  const wallFeet = wallPanelsQty * wallPanelLen;
  const totalWallTrimLF = jTrimLF + cornerTrimLF + (trims.topTrack !== "none" ? topTrackLF : 0);
  const wallScrews = Math.ceil((wallFeet + totalWallTrimLF) / 2);
  const wallScrewBoxes = Math.ceil(wallScrews / SCREWS_PER_BOX);

  const ceilingFeet = ceilingPanelsQty * (ceilingPanelLen || wallPanelLen);
  const ceilingTrimLF = 0; // panels interlock; no dividing trim by default
  const ceilingScrews = Math.ceil((ceilingFeet + ceilingTrimLF) / 1.5);
  const ceilingScrewBoxes = Math.ceil(ceilingScrews / SCREWS_PER_BOX);

  const result: Result = {
    wallPanels: { qty: wallPanelsQty, length: wallPanelLen, width: ref.panelWidth },
    ceilingPanels: ceilingPanelsQty
      ? { qty: ceilingPanelsQty, length: Math.ceil(ceilingPanelLen || wallPanelLen), width: ref.panelWidth }
      : undefined,
    trims: {
      jTrim: { lf: jTrimLF, packs: jPacks, packLen: trims.jTrimPackLen },
      cornerTrim: { lf: cornerTrimLF, packs: cornerPacks, packLen: trims.cornerTrimPackLen },
      ...(trims.topTrack !== "none" ? { topTrack: { lf: topTrackLF, packs: topPacks!, kind: trims.topTrack, packLen: topPackLen } } : {})
    },
    hardware: {
      plugsPacks, spacersPacks, expansionTools,
      wallScrewBoxes, ceilingScrewBoxes
    }
  };

  // Optional pricing (if provided) -> simple bucket placeholders (all zeros unless you wire SKUs/rates)
  if (pricing) {
    result.nsdBuckets = { RELINE: 0, RELINEPRO: 0, Specialty: 0, Other: 0, Shipping: 0 };
  }

  return result;
}

// --- Inline checks (commented):
// packsForPanels(0)   === 0
// packsForPanels(150) === 1
// packsForPanels(151) === 2
// packsForPanels(200) === 2
// packsForPanels(201) === 3

/* Example call (car wash + equipment room):
const rooms: Room[] = [
  { id:"wash", name:"Wash Bay", L:110, W:19, H:13, wallPanelLen:14, panelWidth:18,
    ceiling:{ include:true, orientation:"widthwise" } },
  { id:"equip", name:"Equip Room", L:60, W:12, H:13, wallPanelLen:14, panelWidth:18,
    ceiling:{ include:false, orientation:"widthwise" } },
];
const openings: Opening[] = [
  { id:"md1", where:"wash", type:"manDoor", width:3, height:7, count:4, headerHeight:7, sillHeight:0, wrap:"jTrim", recessed:false },
  { id:"win1", where:"wash", type:"window", width:10, height:8, count:5, headerHeight:4, sillHeight:4, wrap:"cornerTrim" },
  // (garage doors would be added here similarly)
];
const trims: TrimChoices = { topTrack:"crownBase", jTrimPackLen:16, cornerTrimPackLen:16 };
const res = calcEstimate(rooms, openings, trims, { contingencyPct:5 });
console.log(res);
*/
// ---------------- Node-only path discovery (no creation, no duplicates) ----------------
type Maybe<T> = T | null;

export interface PathDiscoveryInput {
  estimateNumber: string;       // e.g. "25885"
  wipEstimatingRoot: string;    // e.g. "I:/ACF QUOTES/WIP Estimating"
  wipDesignRoot: string;        // e.g. "I:/WIP Design"
  bomNumber?: string;           // e.g. "135079-01"
}

export interface PathDiscoveryResult {
  // Estimating side
  estimateRangeFolder: Maybe<string>;
  estimateFolder: Maybe<string>;
  drawings: Maybe<string>;
  estimateDocs: Maybe<string>;
  invoicing: Maybe<string>;
  shipping: Maybe<string>;
  estimatePdf: Maybe<string>;
  drawingPdf: Maybe<string>;
  emailPdf: Maybe<string>;

  // Design/SOF side
  bomRangeFolder: Maybe<string>;
  bomFolder: Maybe<string>;         // e.g. .../<BOM>/ (parent of 1-CURRENT)
  currentFolder: Maybe<string>;     // e.g. .../<BOM>/1-CURRENT/
  sofFile: Maybe<string>;           // only if currentFolder exists
}

// Guard: only load fs when running in Node
function _nodeFs() {
  try {
    // eslint-disable-next-line @typescript-eslint/no-var-requires
    const fs = require("fs");
    // eslint-disable-next-line @typescript-eslint/no-var-requires
    const path = require("path");
    return { fs, path };
  } catch {
    throw new Error("resolveExistingPaths() requires Node.js (fs/path not available).");
  }
}

function _norm(p: string) { return p.replace(/\\/g, "/"); }
function _join(...parts: string[]) { const { path } = _nodeFs(); return _norm(path.join(...parts)); }

function _isDir(p: string): boolean {
  const { fs } = _nodeFs();
  try { return fs.statSync(p).isDirectory(); } catch { return false; }
}

function _listDirs(root: string): string[] {
  const { fs } = _nodeFs();
  try {
    return fs.readdirSync(root, { withFileTypes: true })
             .filter(d => d.isDirectory())
             .map(d => _join(root, d.name));
  } catch { return []; }
}

// Parse names like "2025-25800 to 25899" (year optional), return numeric span if possible
function _parseRangeFolderName(name: string): { start: number; end: number } | null {
  // Accept "2025-25800 to 25899", "25800 to 25899", "2025 - 135000 to 135099", etc.
  const m = name.match(/(\d{4})?[^0-9]*?(\d+)\s*to\s*(\d+)/i) || name.match(/(\d+)\s*-\s*(\d+)/i);
  if (!m) return null;
  const start = parseInt(m[2] || m[1], 10);
  const end   = parseInt(m[3] || m[2], 10);
  if (Number.isFinite(start) && Number.isFinite(end) && start <= end) return { start, end };
  return null;
}

function _chooseBestRangeFolder(candidates: string[], value: number): Maybe<string> {
  // Prefer the smallest span that includes the value
  let best: { folder: string; span: number } | null = null;
  for (const f of candidates) {
    const name = f.split("/").pop() || f;
    const r = _parseRangeFolderName(name);
    if (!r) continue;
    if (value >= r.start && value <= r.end) {
      const span = r.end - r.start;
      if (!best || span < best.span) best = { folder: f, span };
    }
  }
  return best?.folder ?? null;
}

function _findEstimateRangeFolder(root: string, estNum: number): Maybe<string> {
  const subdirs = _listDirs(root);
  const inRange = subdirs.filter(d => {
    const name = d.split("/").pop() || d;
    const r = _parseRangeFolderName(name);
    return r && estNum >= r.start && estNum <= r.end;
  });
  if (inRange.length) return _chooseBestRangeFolder(inRange, estNum);

  // Fallback: sometimes range is nested one level down
  for (const d of subdirs) {
    const nested = _listDirs(d);
    const inNested = nested.filter(n => {
      const name = n.split("/").pop() || n;
      const r = _parseRangeFolderName(name);
      return r && estNum >= r.start && estNum <= r.end;
    });
    if (inNested.length) return _chooseBestRangeFolder(inNested, estNum);
  }
  return null;
}

function _findChildByExactName(parent: string, name: string): Maybe<string> {
  const candidates = _listDirs(parent);
  for (const c of candidates) {
    if ((c.split("/").pop() || "").toLowerCase() === name.toLowerCase()) return c;
  }
  return null;
}

function _bomBase(bom: string): number {
  // "135079-01" -> 135079
  const base = bom.split("-")[0];
  return parseInt(base, 10);
}

export function resolveExistingPaths(input: PathDiscoveryInput): PathDiscoveryResult {
  const estNum = parseInt(input.estimateNumber, 10);
  if (!Number.isFinite(estNum)) throw new Error("estimateNumber must be numeric.");

  // --- Estimating side
  let estimateRangeFolder: Maybe<string> = null;
  let estimateFolder: Maybe<string> = null;

  if (_isDir(input.wipEstimatingRoot)) {
    estimateRangeFolder = _findEstimateRangeFolder(_norm(input.wipEstimatingRoot), estNum);

    // If we found a range, look for the child folder named exactly the estimate number
    if (estimateRangeFolder) {
      estimateFolder = _findChildByExactName(estimateRangeFolder, String(estNum));
      // If not found, try all range folders for an exact child match (handles misfiled ranges)
      if (!estimateFolder) {
        const allRanges = _listDirs(_norm(input.wipEstimatingRoot));
        for (const r of allRanges) {
          const child = _findChildByExactName(r, String(estNum));
          if (child) { estimateFolder = child; break; }
        }
      }
    }
  }

  const drawings   = estimateFolder ? _join(estimateFolder, "DRAWINGS")  : null;
  const estimateDocs = estimateFolder ? _join(estimateFolder, "ESTIMATE")  : null;
  const invoicing  = estimateFolder ? _join(estimateFolder, "INVOICING") : null;
  const shipping   = estimateFolder ? _join(estimateFolder, "SHIPPING")  : null;

  // Default filenames (only if parent exists)
  const estimatePdf = estimateDocs ? _join(estimateDocs, `${input.estimateNumber}.pdf`) : null;
  const drawingPdf  = drawings    ? _join(drawings, `${input.estimateNumber} - Drawing.pdf`) : null;
  const emailPdf    = invoicing   ? _join(invoicing, `${input.estimateNumber} - Email 1.pdf`) : null;

  // --- Design/SOF side
  let bomRangeFolder: Maybe<string> = null;
  let bomFolder: Maybe<string> = null;
  let currentFolder: Maybe<string> = null;
  let sofFile: Maybe<string> = null;

  if (input.bomNumber && _isDir(input.wipDesignRoot)) {
    const base = _bomBase(input.bomNumber);
    bomRangeFolder = _findEstimateRangeFolder(_norm(input.wipDesignRoot), base);

    if (bomRangeFolder) {
      bomFolder = _findChildByExactName(bomRangeFolder, input.bomNumber) || null;
      if (bomFolder) {
        const cf = _join(bomFolder, "1-CURRENT");
        currentFolder = _isDir(cf) ? cf : null;
        if (currentFolder) sofFile = _join(currentFolder, `${input.bomNumber}.sof`);
      }
    }
  }

  return {
    estimateRangeFolder,
    estimateFolder,
    drawings,
    estimateDocs,
    invoicing,
    shipping,
    estimatePdf,
    drawingPdf,
    emailPdf,
    bomRangeFolder,
    bomFolder,
    currentFolder,
    sofFile
  };
}

