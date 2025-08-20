export type PartId =
  | 'panel-main'
  | 'j-trim'
  | 'crown-base'
  | 'cove'
  | 'f-trim'
  | 'inside-corner';

export interface PartSpec {
  id: PartId;
  name: string;
  number: string;
  unit: 'EA' | 'LF' | 'SF' | 'PC';
  category: 'panel' | 'trim' | 'accessory';
}

export const PARTS: Record<PartId, PartSpec> = {
  'panel-main': {
    id: 'panel-main',
    name: 'Main Panel',
    number: 'PNL-001',
    unit: 'PC',
    category: 'panel',
  },
  'j-trim': {
    id: 'j-trim',
    name: 'J Trim',
    number: 'TRM-J',
    unit: 'LF',
    category: 'trim',
  },
  'crown-base': {
    id: 'crown-base',
    name: 'Crown/Base Trim',
    number: 'TRM-CB',
    unit: 'LF',
    category: 'trim',
  },
  cove: {
    id: 'cove',
    name: 'Cove Trim',
    number: 'TRM-CV',
    unit: 'LF',
    category: 'trim',
  },
  'f-trim': {
    id: 'f-trim',
    name: 'F Trim',
    number: 'TRM-F',
    unit: 'LF',
    category: 'trim',
  },
  'inside-corner': {
    id: 'inside-corner',
    name: 'Inside Corner',
    number: 'TRM-IC',
    unit: 'LF',
    category: 'trim',
  },
};
