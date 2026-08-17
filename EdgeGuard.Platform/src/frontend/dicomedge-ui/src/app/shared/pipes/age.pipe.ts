import { Pipe, PipeTransform } from '@angular/core';

/**
 * Computes the current age in whole years from a birth date. Accepts an ISO
 * date string (`yyyy-MM-dd`, as serialized from the backend `DateOnly`) or a
 * `Date`. Returns `null` when the value is missing, unparseable or lies in the
 * future, so templates can decide how to render the absence.
 */
export function computeAge(value: string | Date | null | undefined): number | null {
  if (!value) return null;

  const birth = typeof value === 'string' ? new Date(value) : value;
  if (Number.isNaN(birth.getTime())) return null;

  const now = new Date();
  let age = now.getFullYear() - birth.getFullYear();
  const monthDiff = now.getMonth() - birth.getMonth();
  if (monthDiff < 0 || (monthDiff === 0 && now.getDate() < birth.getDate())) {
    age--;
  }

  return age >= 0 ? age : null;
}

@Pipe({
  name: 'age',
  pure: true,
})
export class AgePipe implements PipeTransform {
  transform(value: string | Date | null | undefined): number | null {
    return computeAge(value);
  }
}
