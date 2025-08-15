// src/shared/customerInfoBridge.ts
import { validatePayload, toExcelWrites, toMaximizerWrites, uiFields } from './ea_field_map';

// This is the single payload your FE should build using uiFields IDs.
// Example shape (values come from your form):
// { estimate_name: '...', estimate_number: '...', currency: 'US Dollar', ... }

export type EAWritePlan = {
  excel: { sheet: string; a1: string; value: unknown; note?: string }[];
  maximizer: { key: string; value: unknown }[];
  errors: string[];
};

export function buildWritePlan(payload: Record<string, unknown>): EAWritePlan {
  const errors = validatePayload(payload);
  if (errors.length) return { excel: [], maximizer: [], errors };

  return {
    excel: toExcelWrites(payload),         // [{ sheet:'Customer Info', a1:'C7', value:'...' }, ...]
    maximizer: toMaximizerWrites(payload), // [{ key:'Opportunity.Subject', value:'...' }, ...]
    errors: []
  };
}

// Optional helper to show your FE team exactly which fields to render.
export const customerInfoUiFields = uiFields;
