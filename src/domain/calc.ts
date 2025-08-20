import { CONTINGENCY } from './settings.js';

export type OpeningTreatment = 'WRAPPED' | 'BUTT';

export interface BuildingInput {
  mode: 'ROOM' | 'WALL';
  length: number; // feet
  width: number; // feet (if WALL mode, set width=1)
  height: number; // feet
  openings: Array<{
    type: 'garage' | 'man' | 'window' | 'custom';
    width: number;
    height: number;
    count: number;
    treatment: OpeningTreatment;
  }>;
  panelCoverageWidthFt: number; // effective panel width in ft
  extraPercent?: number; // override at runtime
  trims: {
    jTrimEnabled: boolean;
    ceilingTransition?: 'crown-base' | 'cove' | 'f-trim' | null;
  };
}

export interface PanelCalcResult {
  basePanels: number;
  extraPercentApplied: number;
  roundedPanels: number;
  overagePercentRounded: number;
  warnExceedsConfigured: boolean;
  manualExtraOverride: boolean;
}

export interface CalcEstimateResult {
  panels: PanelCalcResult;
  trims: {
    jTrimLF: number;
    ceilingTrimLF: number;
    ceilingTransition: 'crown-base' | 'cove' | 'f-trim' | null;
  };
  insideCorners: number;
}

export function computeInsideCorners(input: BuildingInput): number {
  if (input.mode === 'ROOM') {
    if (input.length > 1 && input.width > 1) return 4;
    if (input.width === 1) return 0; // single wall
  }
  return 0;
}

function roundPanels(qty: number): number {
  return qty <= 150 ? Math.ceil(qty / 2) * 2 : Math.ceil(qty / 5) * 5;
}

export function calcEstimate(input: BuildingInput): CalcEstimateResult {
  const perimeter =
    input.mode === 'ROOM' ? 2 * (input.length + input.width) : input.length;

  let openingsArea = 0;
  let openingsPerimeterLF = 0;
  for (const op of input.openings) {
    const area = op.width * op.height * op.count;
    if (op.treatment === 'BUTT') {
      openingsArea += area;
      openingsPerimeterLF += 2 * (op.width + op.height) * op.count;
    }
  }

  const wallArea = perimeter * input.height;
  const netWallArea = wallArea - openingsArea;
  const basePanels = Math.ceil(
    netWallArea / (input.panelCoverageWidthFt * input.height)
  );

  const extraPercent = input.extraPercent ?? CONTINGENCY.defaultExtraPercent;
  const withExtra = basePanels * (1 + extraPercent / 100);
  const roundedPanels = roundPanels(withExtra);
  const overagePercentRounded =
    ((roundedPanels - basePanels) / basePanels) * 100;
  const warnExceedsConfigured =
    overagePercentRounded > CONTINGENCY.warnWhenRoundedExceedsPercent;

  const manualExtraOverride =
    extraPercent !== CONTINGENCY.defaultExtraPercent;

  let jTrimLF = 0;
  let ceilingTrimLF = 0;
  if (input.trims.jTrimEnabled) {
    const multiplier = input.trims.ceilingTransition ? 1 : 3;
    jTrimLF = multiplier * perimeter + openingsPerimeterLF;
  }
  if (input.trims.ceilingTransition) {
    ceilingTrimLF = perimeter;
  }

  return {
    panels: {
      basePanels,
      extraPercentApplied: extraPercent,
      roundedPanels,
      overagePercentRounded,
      warnExceedsConfigured,
      manualExtraOverride,
    },
    trims: {
      jTrimLF,
      ceilingTrimLF,
      ceilingTransition: input.trims.ceilingTransition ?? null,
    },
    insideCorners: computeInsideCorners(input),
  };
}
