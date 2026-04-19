import { Pipe, PipeTransform } from '@angular/core';

const UNITS = ['B', 'KB', 'MB', 'GB', 'TB', 'PB'] as const;

@Pipe({
  name: 'fileSize',
  pure: true,
})
export class FileSizePipe implements PipeTransform {
  transform(bytes: number | null | undefined, decimals = 1): string {
    if (bytes == null || bytes === 0) return '0 B';

    const k = 1024;
    const i = Math.floor(Math.log(Math.abs(bytes)) / Math.log(k));
    const unit = UNITS[Math.min(i, UNITS.length - 1)];
    const value = bytes / Math.pow(k, i);

    return `${value.toFixed(decimals)} ${unit}`;
  }
}
