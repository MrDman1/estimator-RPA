export interface ContingencySettings {
  defaultExtraPercent: number;
  warnWhenRoundedExceedsPercent: number;
}

export const CONTINGENCY: ContingencySettings = {
  defaultExtraPercent: 5,
  warnWhenRoundedExceedsPercent: 7.5,
};
