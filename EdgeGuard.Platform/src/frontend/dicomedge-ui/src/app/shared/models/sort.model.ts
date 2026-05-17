export type SortDirection = 'asc' | 'desc';

export interface SortParams {
  sortBy: string;
  sortDir: SortDirection;
}
