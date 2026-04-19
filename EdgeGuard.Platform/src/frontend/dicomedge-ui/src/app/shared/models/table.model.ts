import { TemplateRef } from '@angular/core';

export interface TableColumn<T> {
  key: string & keyof T | string;
  header: string;
  sortable?: boolean;
  width?: string;
  align?: 'left' | 'center' | 'right';
  cellTemplate?: TemplateRef<{ $implicit: T; value: unknown }>;
  valueAccessor?: (row: T) => unknown;
}
