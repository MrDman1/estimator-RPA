import test from 'node:test';
import assert from 'node:assert';
import { calcEstimate, computeInsideCorners } from '../dist/domain/calc.js';

function baseInput() {
  return {
    mode: 'WALL',
    length: 10,
    width: 1,
    height: 10,
    openings: [],
    panelCoverageWidthFt: 1,
    trims: { jTrimEnabled: true, ceilingTransition: null },
  };
}

test('overage threshold logic', () => {
  const result = calcEstimate(baseInput());
  assert.ok(result.panels.warnExceedsConfigured);
});

test('j-trim reduction when ceiling transition selected', () => {
  const inputBase = {
    mode: 'ROOM',
    length: 10,
    width: 10,
    height: 10,
    openings: [],
    panelCoverageWidthFt: 1,
    trims: { jTrimEnabled: true, ceilingTransition: null },
  };
  const base = calcEstimate(inputBase);
  const withCeiling = calcEstimate({
    ...inputBase,
    trims: { jTrimEnabled: true, ceilingTransition: 'cove' },
  });
  assert.equal(base.trims.jTrimLF, 120);
  assert.equal(withCeiling.trims.jTrimLF, 40);
  assert.equal(withCeiling.trims.ceilingTrimLF, 40);
});

test('inside corner auto rules', () => {
  const room = {
    mode: 'ROOM',
    length: 10,
    width: 10,
    height: 10,
    openings: [],
    panelCoverageWidthFt: 1,
    trims: { jTrimEnabled: true, ceilingTransition: null },
  };
  assert.equal(computeInsideCorners(room), 4);
  const singleWall = { ...room, width: 1 };
  assert.equal(computeInsideCorners(singleWall), 0);
  const wallMode = { ...room, mode: 'WALL', width: 1 };
  assert.equal(computeInsideCorners(wallMode), 0);
});

test('WRAPPED vs BUTT openings', () => {
  const butt = {
    mode: 'WALL',
    length: 20,
    width: 1,
    height: 10,
    openings: [
      { type: 'custom', width: 5, height: 10, count: 1, treatment: 'BUTT' },
    ],
    panelCoverageWidthFt: 1,
    trims: { jTrimEnabled: true, ceilingTransition: null },
  };
  const wrap = {
    ...butt,
    openings: [
      { type: 'custom', width: 5, height: 10, count: 1, treatment: 'WRAPPED' },
    ],
  };
  const buttRes = calcEstimate(butt);
  const wrapRes = calcEstimate(wrap);
  assert.ok(wrapRes.panels.basePanels > buttRes.panels.basePanels);
  assert.equal(buttRes.trims.jTrimLF, 90);
  assert.equal(wrapRes.trims.jTrimLF, 60);
});
