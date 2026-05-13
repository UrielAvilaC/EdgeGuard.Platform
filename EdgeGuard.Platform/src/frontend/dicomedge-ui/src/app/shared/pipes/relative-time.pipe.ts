import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'relativeTime',
  pure: true,
})
export class RelativeTimePipe implements PipeTransform {
  transform(value: string | Date | null | undefined): string {
    if (!value) return '';

    const date = typeof value === 'string' ? new Date(value) : value;
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffSec = Math.floor(diffMs / 1000);
    const diffMin = Math.floor(diffSec / 60);
    const diffHr = Math.floor(diffMin / 60);
    const diffDays = Math.floor(diffHr / 24);

    if (diffSec < 0) return 'en el futuro';
    if (diffSec < 60) return 'hace unos segundos';
    if (diffMin < 60) return diffMin === 1 ? 'hace 1 minuto' : `hace ${diffMin} minutos`;
    if (diffHr < 24) return diffHr === 1 ? 'hace 1 hora' : `hace ${diffHr} horas`;
    if (diffDays < 30) return diffDays === 1 ? 'hace 1 día' : `hace ${diffDays} días`;

    const diffMonths = Math.floor(diffDays / 30);
    if (diffMonths < 12) return diffMonths === 1 ? 'hace 1 mes' : `hace ${diffMonths} meses`;

    const diffYears = Math.floor(diffMonths / 12);
    return diffYears === 1 ? 'hace 1 año' : `hace ${diffYears} años`;
  }
}
