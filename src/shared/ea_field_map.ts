\
// Generated 2025-08-15
export type EAFieldType = 'string' | 'number' | 'enum';

export interface EAExcelLoc { a1: string; merged_note?: string; dropdown_note?: string; }
export interface EAMaximizerLoc { key: string; }
export interface EAField {
  id: string;
  label: string;
  type: EAFieldType;
  required: boolean;
  excel: EAExcelLoc;
  maximizer: EAMaximizerLoc;
  options?: string[];
  format?: 'percent' | 'currency' | 'text';
}

export interface EAFieldMap {
  version: string;
  sheet: string;
  fields: EAField[];
}

export const fieldMap: EAFieldMap = JSON.parse(String.raw`{
  "version": "2025-08-15.1",
  "sheet": "Customer Info",
  "notes": "Canonical field map used by the app to keep FE, Excel, and Maximizer in sync.",
  "fields": [
    {
      "id": "estimate_name",
      "label": "Name of estimate/order",
      "excel": {
        "a1": "C7"
      },
      "maximizer": {
        "key": "Opportunity.Subject"
      },
      "type": "string",
      "required": true
    },
    {
      "id": "estimate_number",
      "label": "Estimate number",
      "excel": {
        "a1": "C9"
      },
      "maximizer": {
        "key": "Opportunity.UserDefined.EstimateNumber"
      },
      "type": "string",
      "required": true
    },
    {
      "id": "revision",
      "label": "Revision #",
      "excel": {
        "a1": "C10"
      },
      "maximizer": {
        "key": "Opportunity.UserDefined.Revision"
      },
      "type": "string",
      "required": false
    },
    {
      "id": "option",
      "label": "Option",
      "excel": {
        "a1": "C11"
      },
      "maximizer": {
        "key": "Opportunity.UserDefined.Option"
      },
      "type": "string",
      "required": false
    },
    {
      "id": "sales_person_line1",
      "label": "Sales person (line 1)",
      "excel": {
        "a1": "O9",
        "merged_note": "O9:T9"
      },
      "maximizer": {
        "key": "Opportunity.Owner.Name"
      },
      "type": "string",
      "required": true
    },
    {
      "id": "sales_person_line2",
      "label": "Sales person (line 2)",
      "excel": {
        "a1": "O10",
        "merged_note": "O10:T10"
      },
      "maximizer": {
        "key": "Opportunity.Owner.Region"
      },
      "type": "string",
      "required": false
    },
    {
      "id": "estimator_line1",
      "label": "Estimator (line 1)",
      "excel": {
        "a1": "O12",
        "merged_note": "O12:T12"
      },
      "maximizer": {
        "key": "Opportunity.UserDefined.Estimator"
      },
      "type": "string",
      "required": true
    },
    {
      "id": "estimator_line2",
      "label": "Estimator (line 2)",
      "excel": {
        "a1": "O13",
        "merged_note": "O13:T13"
      },
      "maximizer": {
        "key": "Opportunity.UserDefined.EstimatorNotes"
      },
      "type": "string",
      "required": false
    },
    {
      "id": "delivery_city_state",
      "label": "City/State of delivery",
      "excel": {
        "a1": "C28"
      },
      "maximizer": {
        "key": "Opportunity.Address.CityState"
      },
      "type": "string",
      "required": true
    },
    {
      "id": "currency",
      "label": "Currency",
      "excel": {
        "a1": "I29",
        "dropdown_note": "I29:J29 (US Dollar | CDN Dollar)"
      },
      "maximizer": {
        "key": "Opportunity.Currency"
      },
      "type": "enum",
      "options": [
        "US Dollar",
        "CDN Dollar"
      ],
      "required": true
    },
    {
      "id": "discount_cf8i",
      "label": "Discount CF8i",
      "excel": {
        "a1": "C40"
      },
      "maximizer": {
        "key": "Opportunity.Discounts.CF8i"
      },
      "type": "number",
      "format": "percent",
      "required": false
    },
    {
      "id": "discount_cf8",
      "label": "Discount CF8",
      "excel": {
        "a1": "C41"
      },
      "maximizer": {
        "key": "Opportunity.Discounts.CF8"
      },
      "type": "number",
      "format": "percent",
      "required": false
    },
    {
      "id": "discount_cf6",
      "label": "Discount CF6",
      "excel": {
        "a1": "C42"
      },
      "maximizer": {
        "key": "Opportunity.Discounts.CF6"
      },
      "type": "number",
      "format": "percent",
      "required": false
    },
    {
      "id": "discount_cf4",
      "label": "Discount CF4",
      "excel": {
        "a1": "C43"
      },
      "maximizer": {
        "key": "Opportunity.Discounts.CF4"
      },
      "type": "number",
      "format": "percent",
      "required": false
    },
    {
      "id": "discount_product",
      "label": "Discount product",
      "excel": {
        "a1": "C44"
      },
      "maximizer": {
        "key": "Opportunity.Discounts.Product"
      },
      "type": "enum",
      "options": [
        "RELINE",
        "RELINEPRO"
      ],
      "required": false
    }
  ]
}`);

export const FieldId = Object.freeze(Object.fromEntries(fieldMap.fields.map(f => [f.id, f.id])));

export function validatePayload(payload: Record<string, unknown>): string[] {
  const errs: string[] = [];
  for (const f of fieldMap.fields) {
    const v = (payload as any)[f.id];
    if (f.required && (v === undefined || v === null || v === '')) {
      errs.push(`Missing required: ${f.id}`);
    }
    if (v !== undefined && v !== null) {
      if (f.type === 'number' && typeof v !== 'number') errs.push(`Expected number for ${f.id}`);
      if (f.type === 'string' && typeof v !== 'string') errs.push(`Expected string for ${f.id}`);
      if (f.type === 'enum') {
        if (typeof v !== 'string' || !(f.options || []).includes(v)) errs.push(`Invalid option for ${f.id}: ${v}`);
      }
    }
  }
  return errs;
}

export const uiFields = fieldMap.fields.map(f => ({
  id: f.id,
  label: f.label,
  type: f.type,
  required: f.required,
  options: f.options ?? undefined
}));

export interface ExcelWrite { sheet: string; a1: string; value: unknown; note?: string }
export function toExcelWrites(payload: Record<string, unknown>): ExcelWrite[] {
  return fieldMap.fields
    .filter(f => (payload as any)[f.id] !== undefined)
    .map(f => ({ sheet: fieldMap.sheet, a1: f.excel.a1, value: (payload as any)[f.id], note: f.excel.merged_note ?? f.excel.dropdown_note }));
}

export interface MaximizerWrite { key: string; value: unknown }
export function toMaximizerWrites(payload: Record<string, unknown>): MaximizerWrite[] {
  return fieldMap.fields
    .filter(f => (payload as any)[f.id] !== undefined)
    .map(f => ({ key: f.maximizer.key, value: (payload as any)[f.id] }));
}
