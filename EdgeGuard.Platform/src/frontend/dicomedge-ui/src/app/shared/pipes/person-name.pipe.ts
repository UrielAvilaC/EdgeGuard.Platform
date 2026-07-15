import { Pipe, PipeTransform } from '@angular/core';

/**
 * Cleans a DICOM person name (PN) for display. DICOM PN uses `^` to separate
 * the five components (Family^Given^Middle^Prefix^Suffix), often padded with
 * trailing carets — e.g. `PEREZ^JUAN^^^`. Replaces the carets with spaces,
 * collapses repeated whitespace and trims, yielding `PEREZ JUAN`.
 *
 * Exported as a plain function so it can be reused outside templates
 * (e.g. computed page titles, dialog messages).
 */
export function cleanPersonName(value: string | null | undefined, fallback = ''): string {
  if (!value) return fallback;
  const cleaned = value
    .replace(/\^/g, ' ')
    .replace(/\s+/g, ' ')
    .trim();
  return cleaned || fallback;
}

@Pipe({
  name: 'personName',
  pure: true,
})
export class PersonNamePipe implements PipeTransform {
  transform(value: string | null | undefined, fallback = ''): string {
    return cleanPersonName(value, fallback);
  }
}
