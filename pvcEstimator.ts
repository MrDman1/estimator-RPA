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

const PANEL_WIDTH_FT = (w: PanelWidth) => (w === 18 ? 1.5 : 1.0);
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
      const qty = Math.ceil((r.L / panelWidthFt) * (1 + contingency));
      ceilingPanelsQty += qty;
      ceilingPanelLen = Math.max(ceilingPanelLen, r.W);
    } else {
      const perRow = Math.ceil(r.W / panelWidthFt);
      const rows = Math.ceil(r.L / (r.ceiling.lengthwisePanelLen ?? r.wallPanelLen));
      const qty = Math.ceil(perRow * rows * (1 + contingency));
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
